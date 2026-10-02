using System.IO;
using System.Text.Json;

namespace TodoApp;

// Plain JSON files under %APPDATA%\SimpleTodo.
public static class Storage
{
    // SIMPLETODO_DIR redirects storage elsewhere, so testing never touches real data.
    public static readonly string Folder =
        Environment.GetEnvironmentVariable("SIMPLETODO_DIR") is { Length: > 0 } custom
            ? custom
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SimpleTodo");

    public static readonly string TodoFile = Path.Combine(Folder, "todos.json");
    public static readonly string ArchiveFile = Path.Combine(Folder, "archive.json");
    public static readonly string RecurringFile = Path.Combine(Folder, "recurring.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static List<T> Load<T>(string path)
    {
        try
        {
            if (!File.Exists(path)) return new List<T>();
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return new List<T>();
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch (Exception)
        {
            // Unreadable or corrupt file: keep the old copy around and start fresh
            // rather than blowing up on launch.
            TryBackup(path);
            return new List<T>();
        }
    }

    public static void Save<T>(string path, IEnumerable<T> items)
    {
        Directory.CreateDirectory(Folder);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(items.ToList(), Options));
        File.Move(temp, path, overwrite: true);
    }

    public static void Append<T>(string path, T item)
    {
        var items = Load<T>(path);
        items.Add(item);
        Save(path, items);
    }

    private static void TryBackup(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Move(path, path + ".bad-" + DateTime.Now.ToString("yyyyMMddHHmmss"), overwrite: true);
        }
        catch (Exception) { }
    }
}
