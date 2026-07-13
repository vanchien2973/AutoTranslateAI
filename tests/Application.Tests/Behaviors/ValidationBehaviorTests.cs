using Application.Behaviors;
using FluentValidation;
using MediatR;

namespace Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record Req(string Name) : IRequest<string>;

    private sealed class ReqValidator : AbstractValidator<Req>
    {
        public ReqValidator() => RuleFor(r => r.Name).NotEmpty();
    }

    private static RequestHandlerDelegate<string> Next(string value) => () => Task.FromResult(value);

    [Fact]
    public async Task Given_InvalidRequest_When_Handle_Then_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<Req, string>([new ReqValidator()]);

        var act = () => behavior.Handle(new Req(""), Next("next"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Given_ValidRequest_When_Handle_Then_CallsNext()
    {
        var behavior = new ValidationBehavior<Req, string>([new ReqValidator()]);

        var result = await behavior.Handle(new Req("ok"), Next("next"), CancellationToken.None);

        result.Should().Be("next");
    }

    [Fact]
    public async Task Given_NoValidators_When_Handle_Then_PassesThrough()
    {
        var behavior = new ValidationBehavior<Req, string>([]);

        var result = await behavior.Handle(new Req(""), Next("next"), CancellationToken.None);

        result.Should().Be("next");
    }
}
