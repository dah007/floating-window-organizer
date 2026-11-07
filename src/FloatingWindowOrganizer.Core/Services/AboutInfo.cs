namespace FloatingWindowOrganizer.Core.Services;

/// <summary>What the About dialog shows: author, links and how the version is displayed.</summary>
public static class AboutInfo
{
    public const string Author = "dah007";

    public const string Tagline = "iOS-style folder widgets for the Windows desktop.";

    public static Uri GitHub { get; } = new("https://github.com/dah007");

    public static Uri LinkedIn { get; } = new("https://www.linkedin.com/in/dah007");

    public static Uri BuyMeACoffee { get; } = new("https://buymeacoffee.com/dah007");

    public static Uri License { get; } = new("https://github.com/dah007/floating-window-organizer/blob/main/LICENSE");

    /// <summary>A link as shown under its label: no scheme, no "www.", no trailing slash.</summary>
    public static string DisplayText(Uri link)
    {
        ArgumentNullException.ThrowIfNull(link);

        var host = link.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? link.Host[4..] : link.Host;
        return (host + link.AbsolutePath).TrimEnd('/');
    }

    /// <summary>
    /// "0.1.0" from an assembly's informational version, dropping the "+commit" suffix the .NET SDK
    /// adds (e.g. "0.1.0+3f2a9c1"). Falls back to "unknown" when there is none.
    /// </summary>
    public static string VersionText(string? informationalVersion)
    {
        var version = informationalVersion?.Split('+', 2)[0].Trim();
        return string.IsNullOrEmpty(version) ? "unknown" : version;
    }
}
