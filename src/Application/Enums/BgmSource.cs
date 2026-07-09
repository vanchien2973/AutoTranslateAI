namespace Application.Enums;

public enum BgmSource
{
    None,                  // No background — the dubbed vocals are the final track.
    DemucsAccompaniment,   // Clean music separated by demucs, mixed at full level.
    DuckedOriginal,        // The original audio, ducked under the dub.
}
