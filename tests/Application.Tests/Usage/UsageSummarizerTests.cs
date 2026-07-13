using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Usage;

public class UsageSummarizerTests
{
    private static UsageRecord Record(string provider, string operation, decimal cost, long input = 100, long output = 0) =>
        new(provider, operation, UsageUnit.Tokens, input, output, cost);

    [Fact]
    public void Given_Records_When_Summarize_Then_TotalsAndGroups()
    {
        var records = new[]
        {
            Record("OpenAI", "Translate", 0.10m, 1000, 500),
            Record("OpenAI", "LlmCompletion", 0.05m, 800, 200),
            Record("Azure", "Tts", 0.16m, 10000),
        };

        var summary = UsageSummarizer.Summarize(records);

        summary.TotalCostUsd.Should().Be(0.31m);
        summary.CallCount.Should().Be(3);
        summary.ByProvider.Should().HaveCount(2);
        summary.ByProvider[0].Key.Should().Be("Azure");   // ordered by cost desc (0.16 > 0.15)
        summary.ByProvider.Single(p => p.Key == "OpenAI").CostUsd.Should().Be(0.15m);
        summary.ByOperation.Should().Contain(op => op.Key == "Translate");
        summary.ByDay.Should().ContainSingle().Which.CallCount.Should().Be(3);
    }

    [Fact]
    public void Given_NoRecords_When_Summarize_Then_Empty()
    {
        var summary = UsageSummarizer.Summarize([]);

        summary.TotalCostUsd.Should().Be(0m);
        summary.CallCount.Should().Be(0);
        summary.ByProvider.Should().BeEmpty();
        summary.ByDay.Should().BeEmpty();
    }
}
