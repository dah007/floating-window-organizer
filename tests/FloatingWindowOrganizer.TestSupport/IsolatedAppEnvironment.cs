using System.Diagnostics;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.TestSupport;

/// <summary>
/// Launches the real app with its own settings folder and single-instance mutex, so smoke/E2E
/// tests never touch the real user's %APPDATA% settings or collide with an instance already
/// running on the machine. See FWO_SETTINGS_DIR / FWO_INSTANCE_ID in App.xaml.cs and
/// SettingsPaths.cs.
/// </summary>
public sealed class IsolatedAppEnvironment : IDisposable
{
    private readonly TempDirectory _settingsDir = new();
    private readonly List<Process> _processes = [];

    public string InstanceId { get; } = Guid.NewGuid().ToString("N");

    public string SettingsFilePath => Path.Combine(_settingsDir.Path, SettingsPaths.FileName);

    /// <summary>
    /// Writes a settings file (unless <paramref name="seedSettings"/> is false) and launches the
    /// app pointed at this isolated environment. Writing a settings file up front — even an empty
    /// one — keeps the app from treating the launch as a first run and popping the folder picker.
    /// </summary>
    public Process Launch(
        IEnumerable<FolderWindowSettings>? widgets = null,
        bool widgetsHidden = false,
        bool seedSettings = true)
    {
        if (seedSettings)
        {
            var settings = new AppSettings { WidgetsHidden = widgetsHidden };
            if (widgets is not null)
            {
                settings.Windows.AddRange(widgets);
            }

            new JsonSettingsStore(SettingsFilePath).Save(settings);
        }

        var startInfo = new ProcessStartInfo(AppExeLocator.Find())
        {
            UseShellExecute = false,
        };
        startInfo.Environment["FWO_SETTINGS_DIR"] = _settingsDir.Path;
        startInfo.Environment["FWO_INSTANCE_ID"] = InstanceId;

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the app process.");
        _processes.Add(process);
        return process;
    }

    public AppSettings ReadSettings() => new JsonSettingsStore(SettingsFilePath).Load();

    public void Dispose()
    {
        foreach (var process in _processes)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(2000);
                }
            }
            catch (InvalidOperationException)
            {
                // Already exited between the check and the kill; nothing to do.
            }
            finally
            {
                process.Dispose();
            }
        }

        _settingsDir.Dispose();
    }
}
