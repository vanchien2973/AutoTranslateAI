using Domain.Enums;

namespace Application.Helpers;

public static class BgmPlanner
{
    public static BgmMixPlan Resolve(BgmMode mode, int duckingDb) => mode switch
    {
        BgmMode.None => new BgmMixPlan(BgmSource.None, 0),
        BgmMode.Duck => new BgmMixPlan(BgmSource.DuckedOriginal, duckingDb),
        _ => new BgmMixPlan(BgmSource.DemucsAccompaniment, 0), // DemucsAI
    };
}
