using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Registry.Models
{
    public class ApiKey
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ServerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Active { get; set; } = true;

        public string K { get; set; } = string.Empty;

        [JsonIgnore]
        public byte[] Key => string.IsNullOrEmpty(K) ? [] : Base64UrlDecode(K);

        public ApiKey(string serverName)
        {
            ServerName = serverName;
            GenerateNewKey();
        }

        public void GenerateNewKey()
        {
            var keyBytes = new byte[32]; // 256-bit AES key
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(keyBytes);
            K = Base64UrlEncode(keyBytes);
        }

        public string ExportJwk()
        {
            var jwk = new
            {
                kty = "oct",
                k = K,
                alg = "A256GCM",
                ext = true,
                key_ops = new[] { "encrypt", "decrypt" }
            };

            return JsonSerializer.Serialize(jwk);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string padded = input
                .Replace('-', '+')
                .Replace('_', '/');

            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
                case 0: break;
                default: throw new FormatException("Invalid base64url string.");
            }

            return Convert.FromBase64String(padded);
        }
    }
}
