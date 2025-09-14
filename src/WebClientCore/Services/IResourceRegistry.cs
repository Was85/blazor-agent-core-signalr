using WebClientCore.State;

namespace WebClientCore.Services;

public interface IResourceRegistry
{
    event Action? Changed;

    void UpsertAgent(string agentId, string name, DateTimeOffset lastSeen);
    void UpsertResources(string agentId, IEnumerable<(string cityId, string name)> cities);
    void ApplyWeatherUpdate(string cityId, string agentId, double tempC, DateTimeOffset timestamp);

    RegistrySnapshot GetSnapshot();
}