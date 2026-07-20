using Microsoft.Data.SqlClient;
using TicketApplication.Services;
using Xunit;

namespace TicketApplication.Tests
{
    // tests für den aufbau des connection-strings (setup / einstellungen)
    public class AppConfigTests
    {
        [Fact]
        public void Windows_Auth_ergibt_integrated_security_ohne_passwort()
        {
            var cs = AppConfigService.BuildConnectionString(new DbConfig
            {
                Server = "sqlserver01",
                Database = "TicketDB",
                UseWindowsAuth = true
            });

            var parsed = new SqlConnectionStringBuilder(cs);
            Assert.True(parsed.IntegratedSecurity);
            Assert.Equal("sqlserver01", parsed.DataSource);
            Assert.Equal("TicketDB", parsed.InitialCatalog);
            Assert.Equal(string.Empty, parsed.Password);
        }

        [Fact]
        public void Sql_Auth_ergibt_user_und_passwort()
        {
            var cs = AppConfigService.BuildConnectionString(new DbConfig
            {
                Server = "sqlserver01",
                Database = "TicketDB",
                UseWindowsAuth = false,
                User = "ticket_app",
                Password = "Geheim123!"
            });

            var parsed = new SqlConnectionStringBuilder(cs);
            Assert.False(parsed.IntegratedSecurity);
            Assert.Equal("ticket_app", parsed.UserID);
            Assert.Equal("Geheim123!", parsed.Password);
        }

        [Fact]
        public void Sonderzeichen_im_passwort_ueberleben_den_roundtrip()
        {
            // semikolon/anführungszeichen dürfen den string nicht zerlegen
            var pw = "P;a'ss\"w=ort";
            var cs = AppConfigService.BuildConnectionString(new DbConfig
            {
                Server = "srv",
                Database = "db",
                UseWindowsAuth = false,
                User = "u",
                Password = pw
            });

            Assert.Equal(pw, new SqlConnectionStringBuilder(cs).Password);
        }

        [Fact]
        public void TrustServerCertificate_ist_gesetzt()
        {
            var cs = AppConfigService.BuildConnectionString(new DbConfig
            {
                Server = "srv",
                Database = "db",
                UseWindowsAuth = true
            });

            Assert.True(new SqlConnectionStringBuilder(cs).TrustServerCertificate);
        }
    }

    // tests für die smtp-einsatzbereitschaft
    public class SmtpSettingsTests
    {
        [Fact]
        public void Nicht_nutzbar_ohne_host()
        {
            var s = new SmtpSettings { Enabled = true, Host = "", FromAddress = "a@b.de" };
            Assert.False(s.IsUsable);
        }

        [Fact]
        public void Nicht_nutzbar_ohne_absender()
        {
            var s = new SmtpSettings { Enabled = true, Host = "smtp.firma.de", FromAddress = "" };
            Assert.False(s.IsUsable);
        }

        [Fact]
        public void Nicht_nutzbar_wenn_deaktiviert()
        {
            var s = new SmtpSettings { Enabled = false, Host = "smtp.firma.de", FromAddress = "a@b.de" };
            Assert.False(s.IsUsable);
        }

        [Fact]
        public void Nutzbar_mit_host_absender_und_aktiv()
        {
            var s = new SmtpSettings { Enabled = true, Host = "smtp.firma.de", FromAddress = "a@b.de" };
            Assert.True(s.IsUsable);
        }
    }
}
