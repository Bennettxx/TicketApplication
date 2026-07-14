using System.Security.Cryptography;
using System.Text;
using TicketApplication.Functions;
using Xunit;

namespace TicketApplication.Tests
{
    // tests für die aes-gcm-verschlüsselung der privaten anhänge
    public class FileCryptoTests
    {
        private static byte[] NeuerKey() => RandomNumberGenerator.GetBytes(32);

        [Fact]
        public void Encrypt_dann_Decrypt_liefert_Original()
        {
            var key = NeuerKey();
            var daten = Encoding.UTF8.GetBytes("streng geheimer Anhang-Inhalt äöü 123");

            var verschluesselt = FileCrypto.Encrypt(daten, key);
            var entschluesselt = FileCrypto.Decrypt(verschluesselt, key);

            Assert.Equal(daten, entschluesselt);
        }

        [Fact]
        public void Encrypt_liefert_bei_gleichem_Input_unterschiedliche_Ausgaben()
        {
            // zufällige nonce -> zwei verschlüsselungen desselben klartexts dürfen nie identisch sein
            var key = NeuerKey();
            var daten = Encoding.UTF8.GetBytes("gleicher Inhalt");

            var a = FileCrypto.Encrypt(daten, key);
            var b = FileCrypto.Encrypt(daten, key);

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Decrypt_mit_manipulierten_Daten_schlaegt_fehl()
        {
            var key = NeuerKey();
            var verschluesselt = FileCrypto.Encrypt(Encoding.UTF8.GetBytes("Original"), key);

            // ein byte im ciphertext kippen -> gcm-integritätsprüfung muss anschlagen
            var bytes = Convert.FromBase64String(verschluesselt);
            bytes[^1] ^= 0xFF;
            var manipuliert = Convert.ToBase64String(bytes);

            Assert.ThrowsAny<CryptographicException>(() => FileCrypto.Decrypt(manipuliert, key));
        }

        [Fact]
        public void Decrypt_mit_falschem_Schluessel_schlaegt_fehl()
        {
            var verschluesselt = FileCrypto.Encrypt(Encoding.UTF8.GetBytes("Original"), NeuerKey());

            Assert.ThrowsAny<CryptographicException>(() => FileCrypto.Decrypt(verschluesselt, NeuerKey()));
        }

        [Fact]
        public void Encrypt_funktioniert_auch_mit_leerem_Inhalt()
        {
            var key = NeuerKey();
            var verschluesselt = FileCrypto.Encrypt(Array.Empty<byte>(), key);

            Assert.Empty(FileCrypto.Decrypt(verschluesselt, key));
        }
    }
}
