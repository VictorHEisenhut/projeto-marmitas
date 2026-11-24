using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Marmitas.Infrastructure.Payments;

public sealed class IntegrationSecretProtector
{
    private readonly byte[] _key;

    public IntegrationSecretProtector(IConfiguration configuration)
    {
        var material = configuration["Payments:TokenEncryptionKey"];
        if (string.IsNullOrWhiteSpace(material))
            material = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(material))
            material = "marmitas-dev-integration-key";

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(material));
    }

    public string Protect(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        var payload = new byte[nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipher, 0, payload, nonce.Length + tag.Length, cipher.Length);
        return "enc:" + Convert.ToBase64String(payload);
    }

    public string Unprotect(string stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return string.Empty;

        if (!stored.StartsWith("enc:", StringComparison.Ordinal))
            return stored;

        var payload = Convert.FromBase64String(stored[4..]);
        if (payload.Length < 29)
            throw new CryptographicException("Segredo de integração inválido.");

        var nonce = payload[..12];
        var tag = payload[12..28];
        var cipher = payload[28..];
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
