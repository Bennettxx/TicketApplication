using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Threading.Channels;
using TicketApplication.Data;
using TicketApplication.Models;

namespace TicketApplication.Services
{
    // smtp-einstellungen, gelesen aus der AppSettings-tabelle
    public class SmtpSettings
    {
        public bool Enabled { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = false;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // entschlüsselt
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "Ticket System";

        public bool IsUsable =>
            Enabled && !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
    }

    // wartende mail in der queue
    public record QueuedMail(string To, string Subject, string Body);

    // in-memory-warteschlange, entkoppelt requests vom smtp-versand
    public class MailQueue
    {
        private readonly Channel<QueuedMail> _channel = Channel.CreateUnbounded<QueuedMail>();

        public void Enqueue(string to, string subject, string body) =>
            _channel.Writer.TryWrite(new QueuedMail(to, subject, body));

        public IAsyncEnumerable<QueuedMail> ReadAllAsync(CancellationToken ct) =>
            _channel.Reader.ReadAllAsync(ct);
    }

    // liest smtp-config aus der db und verschickt mails über mailkit
    public class MailService
    {
        private readonly ApplicationDbContext _context;
        private readonly LogService _log;

        public MailService(ApplicationDbContext context, LogService log)
        {
            _context = context;
            _log = log;
        }

        // alle smtp-keys aus der db laden, passwort entschlüsseln
        public async Task<SmtpSettings> GetSettingsAsync()
        {
            var dict = await _context.AppSettings
                .Where(s => s.Key.StartsWith("Smtp."))
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var s = new SmtpSettings
            {
                Enabled = dict.GetValueOrDefault("Smtp.Enabled") == "true",
                Host = dict.GetValueOrDefault("Smtp.Host") ?? string.Empty,
                UseSsl = dict.GetValueOrDefault("Smtp.UseSsl") == "true",
                User = dict.GetValueOrDefault("Smtp.User") ?? string.Empty,
                FromAddress = dict.GetValueOrDefault("Smtp.From") ?? string.Empty,
                FromName = dict.GetValueOrDefault("Smtp.FromName") ?? "Ticket System"
            };
            if (int.TryParse(dict.GetValueOrDefault("Smtp.Port"), out var port)) s.Port = port;

            var encPw = dict.GetValueOrDefault("Smtp.Password");
            if (!string.IsNullOrEmpty(encPw))
            {
                try { s.Password = SecretProtector.Unprotect(encPw); }
                catch { s.Password = string.Empty; }
            }
            return s;
        }

        // smtp einsatzbereit?
        public async Task<bool> IsConfiguredAsync()
        {
            var s = await GetSettingsAsync();
            return s.IsUsable;
        }

        // einstellungen speichern; passwort nur überschreiben wenn eins übergeben wurde
        public async Task SaveSettingsAsync(SmtpSettings s, string? newPassword)
        {
            async Task Set(string key, string value)
            {
                var row = await _context.AppSettings.FindAsync(key);
                if (row == null) _context.AppSettings.Add(new AppSetting { Key = key, Value = value });
                else row.Value = value;
            }

            await Set("Smtp.Enabled", s.Enabled ? "true" : "false");
            await Set("Smtp.Host", s.Host);
            await Set("Smtp.Port", s.Port.ToString());
            await Set("Smtp.UseSsl", s.UseSsl ? "true" : "false");
            await Set("Smtp.User", s.User);
            await Set("Smtp.From", s.FromAddress);
            await Set("Smtp.FromName", s.FromName);
            if (newPassword != null)
                await Set("Smtp.Password", newPassword.Length == 0 ? string.Empty : SecretProtector.Protect(newPassword));

            await _context.SaveChangesAsync();
        }

        // mail synchron verschicken; wirft bei fehler (aufrufer entscheidet)
        public async Task SendAsync(string to, string subject, string body)
        {
            var s = await GetSettingsAsync();
            if (!s.IsUsable)
                throw new InvalidOperationException("SMTP ist nicht konfiguriert oder deaktiviert.");

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(s.FromName, s.FromAddress));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;
            msg.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            var modus = s.UseSsl ? "SSL direkt" : "STARTTLS";
            _log.Debug(LogBereich.Mail, $"SMTP: verbinde zu {s.Host}:{s.Port} ({modus})");
            await client.ConnectAsync(s.Host, s.Port,
                s.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);
            _log.Debug(LogBereich.Mail, $"SMTP: verbunden, Server meldet '{client.Capabilities}'");
            if (!string.IsNullOrWhiteSpace(s.User))
            {
                _log.Debug(LogBereich.Mail, $"SMTP: authentifiziere als '{s.User}'");
                await client.AuthenticateAsync(s.User, s.Password);
                _log.Debug(LogBereich.Mail, "SMTP: Authentifizierung akzeptiert");
            }
            var antwort = await client.SendAsync(msg);
            _log.Debug(LogBereich.Mail, $"SMTP: Nachricht an {to} übergeben, Server-Antwort: {antwort}");
            await client.DisconnectAsync(true);
            _log.Debug(LogBereich.Mail, "SMTP: Verbindung sauber getrennt (QUIT)");
        }
    }

    // arbeitet die mail-queue im hintergrund ab
    public class MailDispatcherService : BackgroundService
    {
        private readonly MailQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly LogService _log;

        public MailDispatcherService(MailQueue queue, IServiceScopeFactory scopeFactory, LogService log)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var mail in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _log.Debug(LogBereich.Mail, $"Queue: Nachricht entnommen (An: {mail.To}, Betreff: {mail.Subject})");
                    using var scope = _scopeFactory.CreateScope();
                    var mailService = scope.ServiceProvider.GetRequiredService<MailService>();
                    await mailService.SendAsync(mail.To, mail.Subject, mail.Body);
                    _log.Info(LogBereich.Mail, $"Mail an {mail.To} gesendet: {mail.Subject}");
                }
                catch (Exception ex)
                {
                    _log.Error(LogBereich.Mail, $"Mail an {mail.To} fehlgeschlagen: {mail.Subject}", ex);
                }
            }
        }
    }
}
