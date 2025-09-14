using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using WebClientCore.Hubs;

namespace WebClientCore.Services;

public class AgentBridge(ILogger<AgentBridge> logger, IHubContext<ResourceHub> hubContext) : IAgentBridge
{
    private readonly ConcurrentDictionary<string, string> _agentToConnection = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingDetails = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<(bool ok, string? message)>> _pendingSaves = new();

    public void OnAgentRegistered(string agentId, string connectionId)
    {
        _agentToConnection[agentId] = connectionId;
        logger.LogInformation("Mapped agent {AgentId} -> {ConnectionId}", agentId, connectionId);
    }

    public void OnAgentDisconnected(string connectionId)
    {
        foreach (var kvp in _agentToConnection.ToArray())
        {
            if (string.Equals(kvp.Value, connectionId, StringComparison.Ordinal))
            {
                _agentToConnection.TryRemove(kvp.Key, out _);
                logger.LogInformation("Removed mapping for agent {AgentId} due to disconnect", kvp.Key);
            }
        }
    }

    public void DeliverCityDetails(string correlationId, string html)
    {
        logger.LogInformation("DeliverCityDetails: correlationId={CorrelationId}, htmlLength={Length}", correlationId, html?.Length ?? 0);
        if (_pendingDetails.TryRemove(correlationId, out var tcs))
        {
            tcs.TrySetResult(html ?? string.Empty);
        }
    }

    public void DeliverSaveResult(string correlationId, bool ok, string? message)
    {
        logger.LogInformation("DeliverSaveResult: correlationId={CorrelationId}, ok={Ok}, message={Message}", correlationId, ok, message);
        if (_pendingSaves.TryRemove(correlationId, out var tcs))
        {
            tcs.TrySetResult((ok, message));
        }
    }

    public async Task<string> RequestCityDetailsAsync(string agentId, string cityId, TimeSpan? timeout = null)
    {
        if (!_agentToConnection.TryGetValue(agentId, out var connectionId))
            throw new InvalidOperationException($"Agent {agentId} not connected");

        var correlationId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingDetails[correlationId] = tcs;

        logger.LogInformation("Requesting city details from agent {AgentId} via connection {ConnectionId} (cityId={CityId}, correlation={CorrelationId})",
            agentId, connectionId, cityId, correlationId);

        await hubContext.Clients.Client(connectionId).SendAsync(
            "RequestCityDetails",
            correlationId,
            cityId);

        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
        using var reg = cts.Token.Register(() =>
        {
            if (_pendingDetails.TryRemove(correlationId, out var pending))
            {
                pending.TrySetException(new TimeoutException("Timed out waiting for city details"));
            }
        });

        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task<(bool ok, string? message)> RequestSaveAsync(string agentId, string? payload = null, TimeSpan? timeout = null)
    {
        if (!_agentToConnection.TryGetValue(agentId, out var connectionId))
            throw new InvalidOperationException($"Agent {agentId} not connected");

        var correlationId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<(bool ok, string? message)>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingSaves[correlationId] = tcs;

        logger.LogInformation("Requesting save from agent {AgentId} via connection {ConnectionId} (correlation={CorrelationId})",
            agentId, connectionId, correlationId);

        await hubContext.Clients.Client(connectionId).SendAsync(
            "PerformSave",
            correlationId,
            payload ?? string.Empty);

        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
        using var reg = cts.Token.Register(() =>
        {
            if (_pendingSaves.TryRemove(correlationId, out var pending))
            {
                pending.TrySetException(new TimeoutException("Timed out waiting for save result"));
            }
        });

        return await tcs.Task.ConfigureAwait(false);
    }
}
