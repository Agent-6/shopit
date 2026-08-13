using System.Collections.Concurrent;

namespace ShopIt.Authentication.Application.Mocking;

public enum FlowState
{
    /// <summary>No outcome has been received yet — the page keeps polling.</summary>
    Pending,
    Succeeded,
    Failed,
}

/// <summary>
/// The outcome of an asynchronous, event-driven flow (e.g. password reset, email confirmation).
/// </summary>
/// <param name="RequestId">The correlation id of the originating request.</param>
/// <param name="State">Whether the flow succeeded, failed or is still pending.</param>
/// <param name="Message">An optional human-readable result/error message.</param>
public record FlowStatus(Guid RequestId, FlowState State, string? Message);

public interface IFlowStatusStore
{
    void Set(Guid requestId, FlowStatus status);
    FlowStatus? Get(Guid requestId);
}

/// <summary>
/// In-memory store that lets the Account views poll for the outcome of asynchronous
/// flows driven by Kafka integration events.
/// </summary>
public class FlowStatusStore : IFlowStatusStore
{
    private readonly ConcurrentDictionary<Guid, FlowStatus> _statuses = new();

    public void Set(Guid requestId, FlowStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        _statuses[requestId] = status;
    }

    public FlowStatus? Get(Guid requestId) =>
        _statuses.TryGetValue(requestId, out var status) ? status : null;
}
