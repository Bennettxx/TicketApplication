using System.Security.Cryptography;
using System.Text;

namespace TicketApplication.Services
{
    // verschlüsselt geheimnisse mit windows-dpapi (machine-scope)
    // nur auf diesem rechner entschlüsselbar, schützt config-datei + smtp-passwort
#pragma warning disable CA1416
    public static class SecretProtector
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("TicketApplication.v1");

        // klartext -> base64-geschützt
        public static string Protect(string plaintext)
        {
            var data = Encoding.UTF8.GetBytes(plaintext);
            var enc = ProtectedData.Protect(data, Entropy, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(enc);
        }

        // base64-geschützt -> klartext
        public static string Unprotect(string protectedBase64)
        {
            var data = Convert.FromBase64String(protectedBase64);
            var dec = ProtectedData.Unprotect(data, Entropy, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(dec);
        }

        public static byte[] ProtectBytes(byte[] data) =>
            ProtectedData.Protect(data, Entropy, DataProtectionScope.LocalMachine);

        public static byte[] UnprotectBytes(byte[] data) =>
            ProtectedData.Unprotect(data, Entropy, DataProtectionScope.LocalMachine);
    }
#pragma warning restore CA1416
}
