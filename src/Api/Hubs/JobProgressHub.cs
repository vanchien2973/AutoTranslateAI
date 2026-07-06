using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

public sealed class JobProgressHub : Hub
{
    public static string GroupFor(Guid jobId) => $"job-{jobId}";

    public Task Subscribe(Guid jobId) => Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(jobId));

    public Task Unsubscribe(Guid jobId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupFor(jobId));
}
