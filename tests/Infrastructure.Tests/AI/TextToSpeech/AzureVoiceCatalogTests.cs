using Domain.Enums;
using Infrastructure.AI.TextToSpeech;

namespace Infrastructure.Tests.AI.TextToSpeech;

public class AzureVoiceCatalogTests
{
    [Theory]
    [InlineData("vi", VoiceGender.Female, "vi-VN-HoaiMyNeural")]
    [InlineData("vi", VoiceGender.Male, "vi-VN-NamMinhNeural")]
    [InlineData("en-US", VoiceGender.Male, "en-US-GuyNeural")]
    [InlineData("zh-CN", VoiceGender.Female, "zh-CN-XiaoxiaoNeural")]
    public void Given_LanguageAndGender_When_ResolveVoice_Then_ReturnsExpectedVoice(
        string language, VoiceGender gender, string expected)
    {
        // Act
        var voice = AzureVoiceCatalog.ResolveVoice(language, gender);

        // Assert
        voice.Should().Be(expected);
    }

    [Fact]
    public void Given_UnknownLanguage_When_ResolveVoice_Then_Throws()
    {
        // Act
        var act = () => AzureVoiceCatalog.ResolveVoice("xx", VoiceGender.Female);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Given_KnownLanguage_When_ListVoices_Then_ReturnsFemaleAndMale()
    {
        // Act
        var voices = AzureVoiceCatalog.ListVoices("vi");

        // Assert
        voices.Should().HaveCount(2);
        voices.Should().Contain(v => v.Gender == VoiceGender.Female);
        voices.Should().Contain(v => v.Gender == VoiceGender.Male);
    }

    [Fact]
    public void Given_UnknownLanguage_When_ListVoices_Then_ReturnsEmpty()
    {
        // Act
        var voices = AzureVoiceCatalog.ListVoices("xx");

        // Assert
        voices.Should().BeEmpty();
    }
}
