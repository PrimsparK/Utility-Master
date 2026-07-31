using UtilityMaster.Services;

namespace UtilityMaster.Tests;

public class LocTests
{
    [Fact]
    public void ExistingEnglishKeyReturnsValue()
    {
        Loc.SetLanguage("en");

        Assert.Equal("Nades", Loc.Get("nades"));
    }

    [Fact]
    public void ChineseLanguageReturnsLocalizedValue()
    {
        Loc.SetLanguage("zh");

        Assert.NotEqual("Nades", Loc.Get("nades"));
    }

    [Fact]
    public void UnknownKeyReturnsKey()
    {
        Loc.SetLanguage("en");

        Assert.Equal("missing.key", Loc.Get("missing.key"));
    }

    [Fact]
    public void UnknownLanguageFallsBackToEnglish()
    {
        Loc.SetLanguage("xx");

        Assert.Equal("Nades", Loc.Get("nades"));
        Loc.SetLanguage("en");
    }
}
