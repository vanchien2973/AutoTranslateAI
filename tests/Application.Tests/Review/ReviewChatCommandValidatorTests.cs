using Application.Features.Review.ReviewChat;

namespace Application.Tests.Review;

public class ReviewChatCommandValidatorTests
{
    private readonly ReviewChatCommandValidator _validator = new();

    [Fact]
    public void Given_EmptyMessage_When_Validate_Then_IsInvalid()
    {
        // Act / Assert
        _validator.Validate(new ReviewChatCommand(Guid.NewGuid(), "")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Given_EmptyJobId_When_Validate_Then_IsInvalid()
    {
        // Act / Assert
        _validator.Validate(new ReviewChatCommand(Guid.Empty, "sửa câu 5")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Given_ValidInput_When_Validate_Then_IsValid()
    {
        // Act / Assert
        _validator.Validate(new ReviewChatCommand(Guid.NewGuid(), "sửa câu 5")).IsValid.Should().BeTrue();
    }
}
