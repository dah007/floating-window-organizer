using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Tests;

public class AboutInfoTests
{
    public static TheoryData<string> LinkNames => new() { "GitHub", "LinkedIn", "BuyMeACoffee", "License" };

    private static Uri Link(string name) => name switch
    {
        "GitHub" => AboutInfo.GitHub,
        "LinkedIn" => AboutInfo.LinkedIn,
        "BuyMeACoffee" => AboutInfo.BuyMeACoffee,
        _ => AboutInfo.License,
    };

    [Theory]
    [MemberData(nameof(LinkNames))]
    public void Links_AreAbsoluteHttps(string name)
    {
        var link = Link(name);

        Assert.True(link.IsAbsoluteUri);
        Assert.Equal(Uri.UriSchemeHttps, link.Scheme);
    }

    [Fact]
    public void Links_PointAtTheAuthorsAccounts()
    {
        Assert.Equal("github.com/dah007", AboutInfo.DisplayText(AboutInfo.GitHub));
        Assert.Equal("linkedin.com/in/dah007", AboutInfo.DisplayText(AboutInfo.LinkedIn));
        Assert.Equal("buymeacoffee.com/dah007", AboutInfo.DisplayText(AboutInfo.BuyMeACoffee));
    }

    [Theory]
    [InlineData("https://www.example.com/", "example.com")]
    [InlineData("https://example.com/a/b/", "example.com/a/b")]
    [InlineData("https://WWW.example.com/x", "example.com/x")]
    public void DisplayText_DropsSchemeWwwAndTrailingSlash(string url, string expected)
    {
        Assert.Equal(expected, AboutInfo.DisplayText(new Uri(url)));
    }

    [Theory]
    [InlineData("0.1.0", "0.1.0")]
    [InlineData("0.1.0+3f2a9c1e", "0.1.0")]
    [InlineData("1.2.3-beta.1+abc", "1.2.3-beta.1")]
    [InlineData(" 0.2.0 ", "0.2.0")]
    public void VersionText_DropsBuildMetadata(string informational, string expected)
    {
        Assert.Equal(expected, AboutInfo.VersionText(informational));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("+abc")]
    public void VersionText_Missing_IsUnknown(string? informational)
    {
        Assert.Equal("unknown", AboutInfo.VersionText(informational));
    }
}
