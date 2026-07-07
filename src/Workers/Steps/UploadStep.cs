using Application.Interfaces;
using Application.Pipeline;
using Domain.Enums;

namespace Workers.Steps;

/// <summary>Step 10: Upload the output file to storage (R2) and save the URL/key.</summary>
public sealed class UploadStep : IPipelineStep
{
    private readonly IStorageService _storage;

    public UploadStep(IStorageService storage) => _storage = storage;

    public StepType StepType => StepType.Upload;

    public async Task<StepResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(context.OutputVideoPath))
        {
            return StepResult.Fail("No rendered output to upload.");
        }

        var key = OutputStorageKey.For(context.JobId);
        context.OutputUrl = await _storage.UploadAsync(context.OutputVideoPath, key, cancellationToken);
        context.OutputStorageKey = key;

        return StepResult.Success();
    }
}
