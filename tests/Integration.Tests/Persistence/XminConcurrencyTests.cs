using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Integration.Tests.Persistence;

[Trait("Category", "Integration")]
public sealed class XminConcurrencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }

    private async Task<Guid> SeedJobAsync()
    {
        await using var db = CreateDbContext();
        var job = new DubbingJob("https://youtu.be/x", null, "en", "vi", "vi", true);
        db.DubbingJobs.Add(job);
        await db.SaveChangesAsync();
        return job.Id;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Given_TwoContextsEditSameJob_When_SecondSavesRaw_Then_ThrowsConcurrencyException()
    {
        // Arrange
        var jobId = await SeedJobAsync();
        await using var ctx1 = CreateDbContext();
        await using var ctx2 = CreateDbContext();
        var job1 = await ctx1.DubbingJobs.FirstAsync(j => j.Id == jobId);
        var job2 = await ctx2.DubbingJobs.FirstAsync(j => j.Id == jobId);

        // Act
        job1.UpdateProgress(StepType.Download, 10);
        await ctx1.SaveChangesAsync();
        job2.UpdateProgress(StepType.Download, 20); // ctx2 still holds the pre-update xmin token
        var act = () => ctx2.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Given_ConcurrentEdit_When_SecondSavesViaRepository_Then_ReconcilesAndPersists()
    {
        // Arrange
        var jobId = await SeedJobAsync();
        await using var ctx1 = CreateDbContext();
        await using var ctx2 = CreateDbContext();
        var job1 = await ctx1.DubbingJobs.FirstAsync(j => j.Id == jobId);
        var job2 = await ctx2.DubbingJobs.FirstAsync(j => j.Id == jobId);
        var repository = new DubbingJobRepository(ctx2);

        // Act
        job1.UpdateProgress(StepType.Download, 10);
        await ctx1.SaveChangesAsync();
        job2.UpdateProgress(StepType.Transcribe, 55);
        await repository.SaveChangesAsync(CancellationToken.None); // retry helper adopts DB token and re-saves

        // Assert
        await using var verify = CreateDbContext();
        var persisted = await verify.DubbingJobs.FirstAsync(j => j.Id == jobId);
        persisted.ProgressPercent.Should().Be(55);
        persisted.CurrentStep.Should().Be(StepType.Transcribe);
    }
}
