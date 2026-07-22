namespace Application.Features.Providers.GetProviders;

public sealed record GetProvidersResponse(
    string Tts,
    string SpeechToText,
    string Translation,
    string Storage);
