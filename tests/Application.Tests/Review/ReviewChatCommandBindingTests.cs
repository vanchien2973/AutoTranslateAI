using System.Text.Json;
using Application.Features.Review.ReviewChat;

namespace Application.Tests.Review;

public class ReviewChatCommandBindingTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Given_BodyWithoutJobId_When_Deserialize_Then_BindsMessageAndDefaultsJobId()
    {
        // Act
        var command = JsonSerializer.Deserialize<ReviewChatCommand>("""{"userMessage":"hi"}""", Web);

        // Assert
        command!.UserMessage.Should().Be("hi");
        command.JobId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Given_BodyWithJobId_When_Deserialize_Then_JobIdIsIgnored()
    {
        // Act — jobId is supplied from the route, never the body.
        var command = JsonSerializer.Deserialize<ReviewChatCommand>(
            """{"jobId":"11111111-1111-1111-1111-111111111111","userMessage":"hi"}""", Web);

        // Assert
        command!.JobId.Should().Be(Guid.Empty);
        command.UserMessage.Should().Be("hi");
    }
}
