using Application.Interfaces;
using MediatR;

namespace Application.Features.Languages.GetTranslationLanguages;

public sealed class GetTranslationLanguagesQueryHandler
    : IRequestHandler<GetTranslationLanguagesQuery, GetTranslationLanguagesResponse>
{
    private readonly ITranslationService _translation;

    public GetTranslationLanguagesQueryHandler(ITranslationService translation) => _translation = translation;

    public Task<GetTranslationLanguagesResponse> Handle(
        GetTranslationLanguagesQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new GetTranslationLanguagesResponse(_translation.SupportedLanguages));
}
