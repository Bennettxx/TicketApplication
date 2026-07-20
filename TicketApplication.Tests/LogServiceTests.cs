using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using TicketApplication.Services;
using Xunit;

namespace TicketApplication.Tests
{
    // tests für datei-logging und die 60-tage-bereinigung
    public class LogServiceTests : IDisposable
    {
        // minimale env-attrappe: production, content-root = temp-ordner
        private class TestEnv : IWebHostEnvironment
        {
            public string ApplicationName { get; set; } = "TicketApplication.Tests";
            public string ContentRootPath { get; set; } = string.Empty;
            public string EnvironmentName { get; set; } = "Production";
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
            public string WebRootPath { get; set; } = string.Empty;
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        }

        private readonly string _tempDir;
        private readonly LogService _log;
        private readonly string _logDir;

        public LogServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "TicketAppTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            var env = new TestEnv { ContentRootPath = _tempDir };
            var config = new AppConfigService(new ConfigurationBuilder().Build(), env);
            _log = new LogService(config, env);
            _logDir = config.LogPath;
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        [Fact]
        public void Info_schreibt_datei_mit_bereich_und_datum()
        {
            _log.Info("tests", "Hallo Logdatei");

            var erwartet = Path.Combine(_logDir, $"tests-{DateTime.Now:yyyy-MM-dd}.log");
            Assert.True(File.Exists(erwartet));
            Assert.Contains("Hallo Logdatei", File.ReadAllText(erwartet));
            Assert.Contains("[INFO]", File.ReadAllText(erwartet));
        }

        [Fact]
        public void Debug_schreibt_nichts_wenn_debugmodus_aus()
        {
            _log.Debug("tests", "sollte nicht erscheinen");

            var datei = Path.Combine(_logDir, $"tests-{DateTime.Now:yyyy-MM-dd}.log");
            Assert.False(File.Exists(datei) && File.ReadAllText(datei).Contains("sollte nicht erscheinen"));
        }

        [Fact]
        public void Cleanup_loescht_nur_dateien_aelter_als_60_tage()
        {
            Directory.CreateDirectory(_logDir);
            var alt = Path.Combine(_logDir, "alt-2026-01-01.log");
            var neu = Path.Combine(_logDir, "neu-heute.log");
            File.WriteAllText(alt, "alt");
            File.WriteAllText(neu, "neu");
            File.SetLastWriteTime(alt, DateTime.Now.AddDays(-70));

            var geloescht = _log.Cleanup(60);

            Assert.Equal(1, geloescht);
            Assert.False(File.Exists(alt));
            Assert.True(File.Exists(neu));
        }
    }
}
