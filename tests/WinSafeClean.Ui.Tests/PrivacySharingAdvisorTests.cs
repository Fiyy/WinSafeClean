using WinSafeClean.Ui.ViewModels;

namespace WinSafeClean.Ui.Tests;

public sealed class PrivacySharingAdvisorTests
{
    [Theory]
    [InlineData("full")]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_ReturnsCautionForFullOrBlankPrivacy(string privacyMode)
    {
        var advisor = PrivacySharingAdvisor.Create(privacyMode);

        Assert.True(advisor.NeedsCaution);
        Assert.Contains("local paths", advisor.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ReturnsSharingMessageForRedactedPrivacy()
    {
        var advisor = PrivacySharingAdvisor.Create("redacted");

        Assert.False(advisor.NeedsCaution);
        Assert.Contains("reduced path exposure", advisor.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_NormalizesPrivacyMode()
    {
        var advisor = PrivacySharingAdvisor.Create(" REDACTED ");

        Assert.Equal("redacted", advisor.PrivacyMode);
        Assert.False(advisor.NeedsCaution);
    }
}
