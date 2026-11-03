namespace FloatingWindowOrganizer.Core.Models;

/// <summary>
/// Persisted state for a single floating folder window.
/// </summary>
public sealed class FolderWindowSettings
{
    public const double DefaultWidth = 320;
    public const double DefaultHeight = 400;

    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The folder whose contents the window displays.</summary>
    public string FolderPath { get; set; } = string.Empty;

    public double Left { get; set; }

    public double Top { get; set; }

    public double Width { get; set; } = DefaultWidth;

    public double Height { get; set; } = DefaultHeight;
}
