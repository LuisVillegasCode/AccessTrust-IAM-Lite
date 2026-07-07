namespace AccessTrust.Web.Services.Security;

public interface IFieldEncryptionService
{
    string EncryptToBase64(string plainText);

    string DecryptFromBase64(string encryptedValue);
}