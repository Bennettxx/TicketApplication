using System.ComponentModel.DataAnnotations;
using System.Reflection;
using TicketApplication.DTOs;
using TicketApplication.Functions;
using Xunit;

namespace TicketApplication.Tests
{
    // tests für die eigenen validierungs-attribute und die passwort-regel
    public class ValidierungsAttributeTests
    {
        // hilfsklasse für RequiresField: feld B darf nur mit feld A gesetzt sein
        private class KontaktDummy
        {
            public string? KontaktA { get; set; }

            [RequiresField("KontaktA")]
            public string? KontaktB { get; set; }
        }

        private static ValidationResult? Validiere(ValidationAttribute attribut, object? wert, object instanz)
        {
            var kontext = new ValidationContext(instanz);
            return attribut.GetValidationResult(wert, kontext);
        }

        [Fact]
        public void NotInFuture_lehnt_zukunftsdatum_ab()
        {
            var attribut = new NotInFutureAttribute();
            var uebermorgen = DateTime.UtcNow.AddDays(2);

            Assert.NotNull(Validiere(attribut, uebermorgen, new object()));
        }

        [Fact]
        public void NotInFuture_akzeptiert_heute_und_vergangenheit()
        {
            var attribut = new NotInFutureAttribute();

            Assert.Null(Validiere(attribut, DateTime.UtcNow, new object()));
            Assert.Null(Validiere(attribut, DateTime.UtcNow.AddDays(-30), new object()));
        }

        [Fact]
        public void NotInFuture_laesst_null_durch()
        {
            // pflicht regelt [Required], nicht dieses attribut
            var attribut = new NotInFutureAttribute();

            Assert.Null(Validiere(attribut, null, new object()));
        }

        [Fact]
        public void RequiresField_lehnt_B_ohne_A_ab()
        {
            var dummy = new KontaktDummy { KontaktA = null, KontaktB = "b@firma.de" };
            var attribut = new RequiresFieldAttribute("KontaktA");

            Assert.NotNull(Validiere(attribut, dummy.KontaktB, dummy));
        }

        [Fact]
        public void RequiresField_akzeptiert_B_mit_A()
        {
            var dummy = new KontaktDummy { KontaktA = "a@firma.de", KontaktB = "b@firma.de" };
            var attribut = new RequiresFieldAttribute("KontaktA");

            Assert.Null(Validiere(attribut, dummy.KontaktB, dummy));
        }

        [Fact]
        public void RequiresField_ignoriert_leeres_B()
        {
            var dummy = new KontaktDummy { KontaktA = null, KontaktB = null };
            var attribut = new RequiresFieldAttribute("KontaktA");

            Assert.Null(Validiere(attribut, dummy.KontaktB, dummy));
        }

        // passwort-regel direkt vom echten dto lesen, damit der test die produktive regex prüft
        private static RegularExpressionAttribute PasswortRegel()
        {
            var prop = typeof(RegisterDto).GetProperty(nameof(RegisterDto.Password))!;
            return prop.GetCustomAttribute<RegularExpressionAttribute>()!;
        }

        [Theory]
        [InlineData("abc", false)]                // zu kurz
        [InlineData("nurkleinbuchstaben1", false)]// kein großbuchstabe
        [InlineData("NURGROSS1", false)]          // kein kleinbuchstabe
        [InlineData("KeineZahlen", false)]        // keine ziffer
        [InlineData("Passwort1", true)]           // erfüllt alle regeln
        [InlineData("Sicher123!", true)]          // sonderzeichen erlaubt
        public void Passwortregel_prueft_komplexitaet(string passwort, bool erwartetGueltig)
        {
            Assert.Equal(erwartetGueltig, PasswortRegel().IsValid(passwort));
        }
    }
}
