using Application.Interfaces;
using MediatR;

namespace Application.Features.Voices.GetVoices;

public sealed class GetVoicesQueryHandler : IRequestHandler<GetVoicesQuery, GetVoicesResponse>
{
    private readonly ITtsService _tts;

    public GetVoicesQueryHandler(ITtsService tts) => _tts = tts;

    public async Task<GetVoicesResponse> Handle(GetVoicesQuery request, CancellationToken cancellationToken)
    {
        var voices = await _tts.ListVoicesAsync(request.LanguageCode ?? string.Empty, cancellationToken);
        return new GetVoicesResponse(voices);
    }
}
