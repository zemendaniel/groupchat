using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace groupchat.core;
using System.Text.Json;

public class AppData
{
    public string MAC { get; init; } = "";
    public string Nickname { get; init; } = "";
    public int Port { get; init; }
    public string EncryptedPassword { get; set; } = "";
}

public static class DataStore
{
    private static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GroupChat", "config.json");

    public static void Save(AppData data, string? password = null)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        // Encrypt password if provided (Windows only)
        if (!string.IsNullOrEmpty(password) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var encrypted = DPAPIHelper.Protect(password);
            data.EncryptedPassword = Convert.ToBase64String(encrypted);
        }

        var json = JsonSerializer.Serialize(data);

        var tempFile = Path.Combine(directory!, Path.GetRandomFileName());
        
        using (var fs = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        using (var writer = new StreamWriter(fs))
        {
            writer.Write(json);
        }

        // Atomically replace
        File.Move(tempFile, FilePath, overwrite: true);
    }


    public static (AppData Data, string Password) Load()
    {
        if (!File.Exists(FilePath))
            return (new AppData(), ""); 
        
        try
        {
            var json = File.ReadAllText(FilePath);
            var data = JsonSerializer.Deserialize<AppData>(json) ?? new AppData();

            // For now password saving is Windows only
            if (string.IsNullOrEmpty(data.EncryptedPassword) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return (data, "");

            try
            {
                var encrypted = Convert.FromBase64String(data.EncryptedPassword);
                var password = DPAPIHelper.Unprotect(encrypted);
                return (data, password);
            }
            catch (FormatException)
            {
                // Corrupted base64
                return (data, "");
            }
            catch (CryptographicException)
            {
                // Unprotect failed 
                return (data, "");
            }
        }
        catch (JsonException)
        {
            // Corrupted JSON
            return (new AppData(), "");
        }
        catch (IOException)
        {
            // I/O error
            return (new AppData(), "");
        }
    }

}