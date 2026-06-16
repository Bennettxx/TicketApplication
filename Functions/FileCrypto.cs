using System.Security.Cryptography;

namespace TicketApplication.Functions
{
    // Symmetrische Verschlüsselung (AES-GCM) für private Datei-Anhänge.
    // AES-GCM liefert Vertraulichkeit UND Integrität (authentifiziert).
    //
    // Speicherformat (alles in einem Base64-String):
    //   nonce (12 Byte) | tag (16 Byte) | ciphertext (= Klartextlänge)
    //
    // Der Schlüssel (32 Byte) kommt aus der Konfiguration "Attachments:Key"
    // und wird beim ersten Start automatisch erzeugt (siehe Program.cs).
    public static class FileCrypto
    {
        private const int NonceSize = 12; // 96 Bit, Standard für AES-GCM
        private const int TagSize = 16;   // 128 Bit Auth-Tag

        public static string Encrypt(byte[] plaintext, byte[] key)
        {
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            var result = new byte[NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);
            return Convert.ToBase64String(result);
        }

        public static byte[] Decrypt(string base64, byte[] key)
        {
            var data = Convert.FromBase64String(base64);
            var nonce = data[..NonceSize];
            var tag = data[NonceSize..(NonceSize + TagSize)];
            var ciphertext = data[(NonceSize + TagSize)..];

            var plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return plaintext;
        }
    }
}
