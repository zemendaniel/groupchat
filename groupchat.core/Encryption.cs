using System.Text;
using System.Security.Cryptography;

namespace groupchat.core;

public class Encryption
{
    private const string defaultPassword = "k9GsDFy;FJlZi1}R)q=>OisYLCdWKJ";    // Hard coded default password, not secure, but better than nothing
    private readonly string password;
    private const int SaltSize = 16;       // bytes
    private const int NonceSize = 12;      // bytes, this is like IV
    private const int TagSize = 16;        // bytes, verify correct password was used
    private const int KeySize = 32;        // 256-bit key
    private const int Iterations = 200_000; // iterations
    const int cipherOffset = SaltSize + NonceSize;
    
    public Encryption(string password = "")   
    {
        this.password = string.IsNullOrEmpty(password) ? defaultPassword : password;
    }
    
    private byte[] DeriveKey(byte[] salt)
    {
        var key = new byte[KeySize];
        using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        derive.GetBytes(KeySize).CopyTo(key, 0);
        return key;
    }
    
    public byte[] Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);

        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        // Generate salt + nonce
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        
        var key = DeriveKey(salt);

        try
        {
            var ciphertext = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using (var aes = new AesGcm(key, tag.Length))
            {
                aes.Encrypt(nonce, plainBytes, ciphertext, tag);
            }

            var output = new byte[SaltSize + NonceSize + ciphertext.Length + TagSize];
            Buffer.BlockCopy(salt, 0, output, 0, SaltSize);
            Buffer.BlockCopy(nonce, 0, output, SaltSize, NonceSize);
            Buffer.BlockCopy(ciphertext, 0, output, SaltSize + NonceSize, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, output, SaltSize + NonceSize + ciphertext.Length, TagSize);

            return output;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plainBytes);
        }
    }

    public string Decrypt(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < SaltSize + NonceSize + TagSize)
            throw new ArgumentException("Invalid encrypted data.", nameof(data));

        // Split parts
        var salt = new byte[SaltSize];
        Buffer.BlockCopy(data, 0, salt, 0, SaltSize);

        var nonce = new byte[NonceSize];
        Buffer.BlockCopy(data, SaltSize, nonce, 0, NonceSize);

        var cipherLength = data.Length - cipherOffset - TagSize;
        if (cipherLength < 0) throw new ArgumentException("Invalid encrypted data length.", nameof(data));

        var ciphertext = new byte[cipherLength];
        Buffer.BlockCopy(data, cipherOffset, ciphertext, 0, cipherLength);

        var tag = new byte[TagSize];
        Buffer.BlockCopy(data, cipherOffset + cipherLength, tag, 0, TagSize);

        // Derive key
        var key = DeriveKey(salt);

        try
        {
            var plainBytes = new byte[cipherLength];
            using (var aes = new AesGcm(key, tag.Length))
            {
                aes.Decrypt(nonce, ciphertext, tag, plainBytes);
            }

            return Encoding.UTF8.GetString(plainBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }
}