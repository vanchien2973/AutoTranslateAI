using Application.Interfaces;
using MediatR;

namespace Application.Features.Voices.GetSupportedLanguages;

public sealed class GetSupportedLanguagesQueryHandler
    : IRequestHandler<GetSupportedLanguagesQuery, GetSupportedLanguagesResponse>
{
    private readonly ITtsService _tts;

    public GetSupportedLanguagesQueryHandler(ITtsService tts) => _tts = tts;

    public Task<GetSupportedLanguagesResponse> Handle(GetSupportedLanguagesQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(new GetSupportedLanguagesResponse(_tts.SupportedLanguages));
}
