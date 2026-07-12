using Application.Features.Publishing.GenerateSeoMetadata;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Tests.Publishing;

public class GenerateSeoMetadataQueryHandlerTests
{
    private const string ValidJson = """{"title":"Tiêu đề","description":"Mô tả","tags":["vlog","review"]}""";

    [Fact]
    public async Task Given_MissingJob_When_Handle_Then_ReturnsJobNotFound()
    {
        // Arrange
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DubbingJob?)null);
        var handler = new GenerateSeoMetadataQueryHandler(jobs, Substitute.For<ILlmCompletionService>());

        // Act
        var response = await handler.Handle(new GenerateSeoMetadataQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        response.Status.Should().Be(SeoStatus.JobNotFound);
    }

    [Fact]
    public async Task Given_ValidLlmResponse_When_Handle_Then_ReturnsMetadata()
    {
        // Arrange
        var job = TestJobs.AwaitingReview(TestJobs.Segment(0), TestJobs.Segment(1));
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var llm = Substitute.For<ILlmCompletionService>();
        llm.CompleteJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ValidJson);

        // Act
        var response = await new GenerateSeoMetadataQueryHandler(jobs, llm)
            .Handle(new GenerateSeoMetadataQuery(job.Id), CancellationToken.None);

        // Assert
        response.Status.Should().Be(SeoStatus.Ok);
        response.Metadata!.Title.Should().Be("Tiêu đề");
        response.Metadata.Tags.Should().Contain("vlog");
    }

    [Fact]
    public async Task Given_InvalidLlmResponse_When_Handle_Then_GenerationFailed()
    {
        // Arrange
        var job = TestJobs.AwaitingReview(TestJobs.Segment(0));
        var jobs = Substitute.For<IDubbingJobRepository>();
        jobs.GetAsync(job.Id, Arg.Any<CancellationToken>()).Returns(job);
        var llm = Substitute.For<ILlmCompletionService>();
        llm.CompleteJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("not json");

        // Act
        var response = await new GenerateSeoMetadataQueryHandler(jobs, llm)
            .Handle(new GenerateSeoMetadataQuery(job.Id), CancellationToken.None);

        // Assert
        response.Status.Should().Be(SeoStatus.GenerationFailed);
    }
}
