namespace Application.Messaging;

public sealed record DubbingJobPublishRequested(Guid JobId, IReadOnlyList<PublishTarget> Targets);
