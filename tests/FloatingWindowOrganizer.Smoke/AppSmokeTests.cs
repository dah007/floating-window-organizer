using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.TestSupport;

namespace FloatingWindowOrganizer.Smoke;

/// <summary>
/// Launches the real built app and checks it starts (and, for a second instance, exits) without
/// crashing. Each test gets its own isolated settings folder and single-instance mutex (via
/// IsolatedAppEnvironment) so it never touches the real user's %APPDATA% settings or collides
/// with an instance already running on the machine.
/// </summary>
[Trait("Category", "Smoke")]
public sealed class AppSmokeTests
{
    private static readonly TimeSpan AliveCheckDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void AppStartsAndStaysAlive_WithAnExistingWidget()
    {
        using var folder = new TempDirectory();
        File.WriteAllText(folder.Combine("a.txt"), "a");
        File.WriteAllText(folder.Combine("b.txt"), "b");

        using var env = new IsolatedAppEnvironment();
        var process = env.Launch(widgets: [new FolderWindowSettings { FolderPath = folder.Path }]);

        Thread.Sleep(AliveCheckDelay);

        Assert.False(process.HasExited, "The app exited unexpectedly shortly after startup.");
    }

    [Fact]
    public void AppStartsAndStaysAlive_WithNoWidgets()
    {
        using var env = new IsolatedAppEnvironment();
        // An empty (but present) settings file means this isn't treated as a first run, so the
        // app never pops the folder-picker dialog and waits in the tray with nothing to show.
        var process = env.Launch(widgets: []);

        Thread.Sleep(AliveCheckDelay);

        Assert.False(process.HasExited, "The app exited unexpectedly shortly after startup.");
    }

    [Fact]
    public void SecondInstance_ExitsImmediately_FirstInstanceKeepsRunning()
    {
        using var env = new IsolatedAppEnvironment();
        var first = env.Launch(widgets: []);
        Thread.Sleep(AliveCheckDelay);
        Assert.False(first.HasExited, "The first instance exited unexpectedly shortly after startup.");

        var second = env.Launch(seedSettings: false);
        var exitedInTime = second.WaitForExit(ExitTimeout);

        Assert.True(exitedInTime, "A second instance should shut itself down quickly instead of running alongside the first.");
        Assert.False(first.HasExited, "The first instance should keep running while a second instance starts and exits.");
    }
}
