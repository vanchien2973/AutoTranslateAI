namespace Application.Features.Voices.GetVoices;

public sealed record GetVoicesResponse(IReadOnlyList<VoiceInfo> Voices);
