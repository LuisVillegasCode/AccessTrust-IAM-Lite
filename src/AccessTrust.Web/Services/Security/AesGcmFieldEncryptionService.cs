using System.Security.Cryptography;
using System.Text;

namespace AccessTrust.Web.Services.Security;

public class AesGcmFieldEncryptionService : IFieldEncryptionService
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private const string EnvironmentVariableName = "ACCESS_TRUST_FIELD_ENCRYPTION_KEY_BASE64";

    private readonly byte[] _key;

    public AesGcmFieldEncryptionService()
    {
        var keyBase64 = Environment.GetEnvironmentVariable(EnvironmentVariableName);

        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException(
                $"No se encontró la variable de entorno {EnvironmentVariableName}."
            );
        }

        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length is not (16 or 24 or 32))
        {
            throw new InvalidOperationException(
                "La clave de cifrado debe tener 16, 24 o 32 bytes después de decodificar Base64."
            );
        }
    }

    public string EncryptToBase64(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            throw new ArgumentException("El texto plano no puede estar vacío.", nameof(plainText));
        }

        var plainBytes = Encoding.UTF8.GetBytes(plainText.Trim());
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        return string.Join(":",
            "v1",
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(cipherBytes)
        );
    }

    public string DecryptFromBase64(string encryptedValue)
    {
        if (string.IsNullOrWhiteSpace(encryptedValue))
        {
            return string.Empty;
        }

        var parts = encryptedValue.Split(':');

        if (parts.Length != 4 || parts[0] != "v1")
        {
            throw new InvalidOperationException("El valor cifrado no tiene un formato válido.");
        }

        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var cipherBytes = Convert.FromBase64String(parts[3]);
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}