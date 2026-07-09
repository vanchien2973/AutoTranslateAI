using Domain.Enums;

namespace Application.Tests.Pipeline;

public class BgmPlannerTests
{
    [Fact]
    public void Given_None_When_Resolve_Then_NoBackground()
    {
        BgmPlanner.Resolve(BgmMode.None, -12).Source.Should().Be(BgmSource.None);
    }

    [Fact]
    public void Given_Demucs_When_Resolve_Then_AccompanimentAtFullLevel()
    {
        var plan = BgmPlanner.Resolve(BgmMode.DemucsAI, -12);
        plan.Source.Should().Be(BgmSource.DemucsAccompaniment);
        plan.GainDb.Should().Be(0);
    }

    [Fact]
    public void Given_Duck_When_Resolve_Then_DuckedOriginalAtDuckingGain()
    {
        var plan = BgmPlanner.Resolve(BgmMode.Duck, -12);
        plan.Source.Should().Be(BgmSource.DuckedOriginal);
        plan.GainDb.Should().Be(-12);
    }

    [Fact]
    public void Given_DuckWithCustomGain_When_Resolve_Then_UsesThatGain()
    {
        BgmPlanner.Resolve(BgmMode.Duck, -6).GainDb.Should().Be(-6);
    }
}
