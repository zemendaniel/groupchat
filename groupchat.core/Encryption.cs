using System.Security.Cryptography;

namespace groupchat.core;

public class Encryption
{
    private const string defaultPassword = "k9GsDFy;FJlZi1}R)q=>OisYLCdWKJ";    // Hard coded default password, not secure, but better than nothing
    private readonly string password;
    
    public Encryption(string password = "")   
    {
        this.password = string.IsNullOrWhiteSpace(password) ? defaultPassword : password;
    }
    
    public byte[] Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        aes.GenerateIV();   // Cleaner to generate new AES each time
        // 16 bytes
        var iv = aes.IV;    // Two identical messages will result in different ciphertexts

        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        var result = new byte[16 + cipherBytes.Length];  // Prepend IV
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

        return result;
    }

    public string Decrypt(byte[] data)
    {
        if (data.Length < 16) // IV is 16 bytes
            throw new ArgumentException("Invalid encrypted data");

        var iv = new byte[16];
        var cipherBytes = new byte[data.Length - 16];

        Buffer.BlockCopy(data, 0, iv, 0, 16);
        Buffer.BlockCopy(data, 16, cipherBytes, 0, cipherBytes.Length);

        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decrypter = aes.CreateDecryptor();
        var plainBytes = decrypter.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return System.Text.Encoding.UTF8.GetString(plainBytes);
    } 
}