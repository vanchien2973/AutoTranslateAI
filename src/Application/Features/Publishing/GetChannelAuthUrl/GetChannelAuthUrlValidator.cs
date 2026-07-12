using FluentValidation;

namespace Application.Features.Publishing.GetChannelAuthUrl;

public sealed class GetChannelAuthUrlValidator : AbstractValidator<GetChannelAuthUrlQuery>
{
    public GetChannelAuthUrlValidator()
    {
        RuleFor(query => query.Platform).IsInEnum();
        RuleFor(query => query.RedirectUri)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("RedirectUri must be an absolute URL.");
    }
}
