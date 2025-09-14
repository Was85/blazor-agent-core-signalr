using System.Threading.Tasks;

namespace WebClientCore.Services;

public interface IAgentBridge
{
    // Called by the Hub when an agent registers/unregisters
    void OnAgentRegistered(string agentId, string connectionId);
    void OnAgentDisconnected(string connectionId);

    // Called by the Hub when agent pushes responses
    void DeliverCityDetails(string correlationId, string html);
    void DeliverSaveResult(string correlationId, bool ok, string? message);

    // Called by UI to request actions from agent via server->client callback
    Task<string> RequestCityDetailsAsync(string agentId, string cityId, TimeSpan? timeout = null);
    Task<(bool ok, string? message)> RequestSaveAsync(string agentId, string? payload = null, TimeSpan? timeout = null);
}
