using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace TicketApplication.Services
{
    // db-verbindungsdaten in der geschützten config-datei
    public class DbConfig
    {
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public bool UseWindowsAuth { get; set; } = true;
        public string? User { get; set; }
        public string? Password { get; set; }
    }

    // inhalt der geschützten config-datei
    public class AppConfigData
    {
        public DbConfig? Db { get; set; }
        public string? LogPath { get; set; }
        public bool DebugLogging { get; set; }
    }

    // verwaltet die lokale, dpapi-verschlüsselte config-datei (appconfig.protected)
    // fallback: connection-string aus appsettings.Local.json / umgebungsvariablen
    public class AppConfigService
    {
        private readonly string _filePath;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly object _lock = new();
        private AppConfigData? _data;

        public AppConfigService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
            _filePath = Path.Combine(env.ContentRootPath, "appconfig.protected");
            TryLoad();
        }

        // true wenn eine nutzbare db-verbindung existiert (datei oder fallback)
        public bool IsConfigured =>
            _data?.Db != null ||
            !string.IsNullOrWhiteSpace(_configuration.GetConnectionString("DefaultConnection"));

        // true wenn die verbindung noch aus appsettings/umgebungsvariablen kommt
        public bool IsLegacyFallback =>
            _data?.Db == null &&
            !string.IsNullOrWhiteSpace(_configuration.GetConnectionString("DefaultConnection"));

        public AppConfigData? Current => _data;

        // aktiver connection-string, config-datei hat vorrang
        public string? GetConnectionString()
        {
            var db = _data?.Db;
            if (db != null) return BuildConnectionString(db);
            return _configuration.GetConnectionString("DefaultConnection");
        }

        // log-verzeichnis, default: <app>\Logs
        public string LogPath
        {
            get
            {
                var p = _data?.LogPath;
                if (!string.IsNullOrWhiteSpace(p)) return p;
                return Path.Combine(_env.ContentRootPath, "Logs");
            }
        }

        // verbindungsdaten setzen und speichern
        public void SaveDb(DbConfig db)
        {
            lock (_lock)
            {
                _data ??= new AppConfigData();
                _data.Db = db;
                Persist();
            }
        }

        // log-pfad setzen und speichern
        public void SaveLogPath(string logPath)
        {
            lock (_lock)
            {
                _data ??= new AppConfigData();
                _data.LogPath = logPath;
                Persist();
            }
        }

        // debug-logging (detaillierte technische logs) an/aus
        public bool DebugLogging => _data?.DebugLogging ?? false;

        public void SaveDebugLogging(bool aktiv)
        {
            lock (_lock)
            {
                _data ??= new AppConfigData();
                _data.DebugLogging = aktiv;
                Persist();
            }
        }

        // connection-string sicher über den builder zusammensetzen
        public static string BuildConnectionString(DbConfig db)
        {
            var b = new SqlConnectionStringBuilder
            {
                DataSource = db.Server,
                InitialCatalog = db.Database,
                TrustServerCertificate = true,
                Encrypt = false
            };
            if (db.UseWindowsAuth)
            {
                b.IntegratedSecurity = true;
            }
            else
            {
                b.UserID = db.User ?? string.Empty;
                b.Password = db.Password ?? string.Empty;
            }
            return b.ConnectionString;
        }

        // verbindung testen; erst gegen die ziel-db, sonst gegen master (db wird später angelegt)
        public static (bool Ok, string Message) TestConnection(DbConfig db)
        {
            try
            {
                using var conn = new SqlConnection(BuildConnectionString(db));
                conn.Open();
                return (true, "Verbindung erfolgreich, Datenbank vorhanden.");
            }
            catch (SqlException)
            {
                try
                {
                    var master = new DbConfig
                    {
                        Server = db.Server,
                        Database = "master",
                        UseWindowsAuth = db.UseWindowsAuth,
                        User = db.User,
                        Password = db.Password
                    };
                    using var conn = new SqlConnection(BuildConnectionString(master));
                    conn.Open();
                    return (true, "Server erreichbar. Die Datenbank existiert noch nicht und wird beim Speichern angelegt.");
                }
                catch (Exception ex2)
                {
                    return (false, "Verbindung fehlgeschlagen: " + ex2.Message);
                }
            }
            catch (Exception ex)
            {
                return (false, "Verbindung fehlgeschlagen: " + ex.Message);
            }
        }

        // config-datei lesen und entschlüsseln, bei fehler ignorieren (setup-modus)
        private void TryLoad()
        {
            try
            {
                if (!File.Exists(_filePath)) return;
                var protectedBytes = File.ReadAllBytes(_filePath);
                var json = Encoding.UTF8.GetString(SecretProtector.UnprotectBytes(protectedBytes));
                _data = JsonSerializer.Deserialize<AppConfigData>(json);
            }
            catch
            {
                _data = null;
            }
        }

        // config verschlüsselt auf platte schreiben
        private void Persist()
        {
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = false });
            var protectedBytes = SecretProtector.ProtectBytes(Encoding.UTF8.GetBytes(json));
            File.WriteAllBytes(_filePath, protectedBytes);
        }
    }
}
