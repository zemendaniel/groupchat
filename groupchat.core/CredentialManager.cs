using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace groupchat.core;

public static class DPAPIHelper
{
    public static byte[] Protect(string plaintext)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return [];
        var data = Encoding.UTF8.GetBytes(plaintext);
        return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
    }

    public static string Unprotect(byte[] encryptedData)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "";
        var decrypted = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }
}