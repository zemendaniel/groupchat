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
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        
        if (!string.IsNullOrEmpty(password))
        {
            var encrypted = DPAPIHelper.Protect(password);
            data.EncryptedPassword = Convert.ToBase64String(encrypted);
        }
        
        File.WriteAllText(FilePath, JsonSerializer.Serialize(data));
    }

    public static (AppData Data, string Password) Load()
    {
        if (!File.Exists(FilePath))
            return (new AppData(), ""); 
        
        var data = JsonSerializer.Deserialize<AppData>(File.ReadAllText(FilePath)) ?? new AppData();
        var password = "";
        if (string.IsNullOrEmpty(data.EncryptedPassword)) return (data, password);
        
        var encrypted = Convert.FromBase64String(data.EncryptedPassword);
        password = DPAPIHelper.Unprotect(encrypted);

        return (data, password);
        
    }
}