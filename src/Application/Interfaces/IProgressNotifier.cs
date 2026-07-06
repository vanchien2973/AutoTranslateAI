using Application.Dtos;

namespace Application.Interfaces;

public interface IProgressNotifier
{
    Task ReportAsync(JobProgress progress, CancellationToken cancellationToken);
}
