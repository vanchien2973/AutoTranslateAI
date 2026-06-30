using Infrastructure.AI.Translation;

namespace Infrastructure.Tests.AI.Translation;

public class TranslationResponseParserTests
{
    [Fact]
    public void Given_TranslationsObject_When_Parse_Then_ReturnsStringsInOrder()
    {
        // Arrange
        const string content = "{\"translations\": [\"Xin chào\", \"Thế giới\"]}";

        // Act
        var result = TranslationResponseParser.Parse(content, expectedCount: 2);

        // Assert
        result.Should().ContainInOrder("Xin chào", "Thế giới");
    }

    [Fact]
    public void Given_BareJsonArray_When_Parse_Then_ToleratesItAndReturnsStrings()
    {
        // Arrange
        const string content = "[\"a\", \"b\"]";

        // Act
        var result = TranslationResponseParser.Parse(content, expectedCount: 2);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Given_CountMismatch_When_Parse_Then_Throws()
    {
        // Arrange
        const string content = "{\"translations\": [\"only one\"]}";

        // Act
        var act = () => TranslationResponseParser.Parse(content, expectedCount: 2);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    public void Given_EmptyOrInvalidContent_When_Parse_Then_Throws(string content)
    {
        // Act
        var act = () => TranslationResponseParser.Parse(content, expectedCount: 1);

        // Assert
        act.Should().Throw<Exception>();
    }
}
