using FluentValidation;

namespace Application.Features.Review.ReviewChat;

public sealed class ReviewChatCommandValidator : AbstractValidator<ReviewChatCommand>
{
    public ReviewChatCommandValidator()
    {
        RuleFor(command => command.JobId).NotEmpty();
        RuleFor(command => command.UserMessage).NotEmpty().MaximumLength(2000);
    }
}
