namespace Domain.Enums;

public enum UsageUnit
{
    Tokens = 0,      // LLM (input + output tính riêng)
    Characters = 1,  // TTS
    Seconds = 2,     // STT
}
