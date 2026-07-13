using Application.Features.Usage.GetUsageSummary;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Usage;

public class GetUsageSummaryQueryHandlerTests
{
    [Fact]
    public async Task Given_Records_When_Handle_Then_ReturnsSummary()
    {
        var usage = Substitute.For<IUsageRepository>();
        usage.ListSinceAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<UsageRecord> { new("OpenAI", "Translate", UsageUnit.Tokens, 1000, 500, 0.1m) });
        var handler = new GetUsageSummaryQueryHandler(usage);

        var response = await handler.Handle(new GetUsageSummaryQuery(30), CancellationToken.None);

        response.Days.Should().Be(30);
        response.Summary.TotalCostUsd.Should().Be(0.1m);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1000, 365)]
    public async Task Given_OutOfRangeDays_When_Handle_Then_Clamped(int requested, int expected)
    {
        var usage = Substitute.For<IUsageRepository>();
        usage.ListSinceAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(new List<UsageRecord>());

        var response = await new GetUsageSummaryQueryHandler(usage).Handle(new GetUsageSummaryQuery(requested), CancellationToken.None);

        response.Days.Should().Be(expected);
    }
}
