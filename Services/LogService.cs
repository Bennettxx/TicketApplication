namespace TicketApplication.Services
{
    // log-kategorien = eigene dateien pro funktionsbereich
    public static class LogBereich
    {
        public const string App = "app";                    // start, setup, allgemeines
        public const string Auth = "auth";                  // login, registrierung, passwörter
        public const string Benutzer = "benutzer";          // benutzerverwaltung
        public const string Tickets = "tickets";            // ticket-aktionen
        public const string Mail = "mail";                  // mailversand
        public const string Einstellungen = "einstellungen";// settings-änderungen
        public const string Fehler = "fehler";              // unbehandelte exceptions
    }

    // einfaches datei-logging: pro bereich und tag eine datei im log-ordner
    // format: 2026-07-11 10:00:00 [INFO] text
    public class LogService
    {
        private readonly AppConfigService _config;
        private readonly object _lock = new();

        public LogService(AppConfigService config)
        {
            _config = config;
        }

        public void Info(string bereich, string text) => Write(bereich, "INFO", text);
        public void Warn(string bereich, string text) => Write(bereich, "WARN", text);

        public void Error(string bereich, string text, Exception? ex = null)
        {
            if (ex != null) text += " | " + ex.GetType().Name + ": " + ex.Message + Environment.NewLine + ex.StackTrace;
            Write(bereich, "ERROR", text);
        }

        // zeile anhängen; logging darf die app nie zum absturz bringen
        private void Write(string bereich, string level, string text)
        {
            try
            {
                var dir = _config.LogPath;
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, $"{bereich}-{DateTime.Now:yyyy-MM-dd}.log");
                var zeile = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {text}{Environment.NewLine}";
                lock (_lock)
                {
                    File.AppendAllText(file, zeile);
                }
            }
            catch
            {
            }
        }

        // log-dateien älter als angegebene tage löschen
        public int Cleanup(int maxAlterTage = 60)
        {
            int geloescht = 0;
            try
            {
                var dir = _config.LogPath;
                if (!Directory.Exists(dir)) return 0;
                var grenze = DateTime.Now.AddDays(-maxAlterTage);
                foreach (var file in Directory.GetFiles(dir, "*.log"))
                {
                    if (File.GetLastWriteTime(file) < grenze)
                    {
                        File.Delete(file);
                        geloescht++;
                    }
                }
            }
            catch
            {
            }
            return geloescht;
        }
    }

    // räumt beim start und danach täglich alte logs weg (älter 60 tage)
    public class LogCleanupService : BackgroundService
    {
        private readonly LogService _log;

        public LogCleanupService(LogService log)
        {
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var anzahl = _log.Cleanup(60);
                if (anzahl > 0)
                    _log.Info(LogBereich.App, $"Log-Bereinigung: {anzahl} Datei(en) älter als 60 Tage gelöscht.");
                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}
