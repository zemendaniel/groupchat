using System.Security.Cryptography;
using System.Text;

namespace groupchat.core;

// todo multi platform
public static class DPAPIHelper
{
    public static byte[] Protect(string plaintext)
    {
        var data = Encoding.UTF8.GetBytes(plaintext);
        return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
    }

    public static string Unprotect(byte[] encryptedData)
    {
        var decrypted = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }
}