using System.Security.Cryptography;

namespace TicketApplication.Functions
{
    // aes-gcm für private anhänge, liefert vertraulichkeit + integrität
    // speicherformat als base64: nonce(12) | tag(16) | ciphertext
    // key (32 byte) kommt aus "Attachments:Key", wird beim ersten start erzeugt
    public static class FileCrypto
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;

        // verschlüsselt und packt nonce+tag+ciphertext in einen base64-string
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

        // gegenstück zu Encrypt, wirft bei manipulierten daten
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
