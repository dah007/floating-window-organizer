namespace FloatingWindowOrganizer.TestSupport;

/// <summary>Finds the built app executable so smoke/E2E tests can launch the real thing.</summary>
public static class AppExeLocator
{
    private const string SolutionFileName = "FloatingWindowOrganizer.slnx";
    private const string ExeName = "FloatingWindowOrganizer.exe";

    /// <summary>
    /// Honors FWO_APP_EXE if set; otherwise walks up from the test assembly's output directory to
    /// find the repo root, then returns the most recently built FloatingWindowOrganizer.exe under
    /// src/FloatingWindowOrganizer.App/bin (whichever configuration/RID was last built).
    /// </summary>
    public static string Find()
    {
        var overridePath = Environment.GetEnvironmentVariable("FWO_APP_EXE");
        if (!string.IsNullOrEmpty(overridePath))
        {
            return overridePath;
        }

        var repoRoot = FindRepoRoot(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException(
                $"Couldn't find {SolutionFileName} above {AppContext.BaseDirectory}.");

        var appBinDir = Path.Combine(repoRoot, "src", "FloatingWindowOrganizer.App", "bin");
        if (!Directory.Exists(appBinDir))
        {
            throw new InvalidOperationException($"{appBinDir} doesn't exist; build the App project first.");
        }

        var exe = Directory
            .EnumerateFiles(appBinDir, ExeName, SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        return exe ?? throw new InvalidOperationException(
            $"No {ExeName} found under {appBinDir}; build the App project first.");
    }

    private static string? FindRepoRoot(string startDirectory)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
            {
                return dir.FullName;
            }
        }

        return null;
    }
}
