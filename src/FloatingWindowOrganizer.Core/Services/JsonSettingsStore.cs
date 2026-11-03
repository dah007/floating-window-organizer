using System.Text.Json;
using FloatingWindowOrganizer.Core.Models;

namespace FloatingWindowOrganizer.Core.Services;

/// <summary>
/// Reads and writes <see cref="AppSettings"/> as a JSON file.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public JsonSettingsStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = filePath;
    }

    public string FilePath { get; }

    /// <summary>Path a corrupt settings file is moved to before defaults are used.</summary>
    public string CorruptBackupPath => FilePath + ".corrupt";

    public AppSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return new AppSettings();
        }

        AppSettings? settings;
        try
        {
            using var stream = File.OpenRead(FilePath);
            settings = JsonSerializer.Deserialize<AppSettings>(stream, SerializerOptions);
        }
        catch (JsonException)
        {
            // Keep the unreadable file so the next Save doesn't silently destroy it.
            File.Move(FilePath, CorruptBackupPath, overwrite: true);
            return new AppSettings();
        }

        settings ??= new AppSettings();
        settings.Windows ??= [];
        return settings;
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to a temp file first so a crash mid-write can't leave a half-written settings file.
        var tempPath = FilePath + ".tmp";
        using (var stream = File.Create(tempPath))
        {
            JsonSerializer.Serialize(stream, settings, SerializerOptions);
        }

        File.Move(tempPath, FilePath, overwrite: true);
    }
}
