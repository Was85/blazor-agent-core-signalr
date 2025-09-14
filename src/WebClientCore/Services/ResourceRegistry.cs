using System.Collections.Concurrent;
using WebClientCore.State;

namespace WebClientCore.Services;

public class ResourceRegistry : IResourceRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, AgentState> _agents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CityWeatherState> _cities = new(StringComparer.OrdinalIgnoreCase);

    public event Action? Changed;

    public void UpsertAgent(string agentId, string name, DateTimeOffset lastSeen)
    {
        if (string.IsNullOrWhiteSpace(agentId)) return;

        lock (_gate)
        {
            _agents[agentId] = new AgentState(agentId, name, lastSeen);
        }
        RaiseChanged();
    }

    public void UpsertResources(string agentId, IEnumerable<(string cityId, string name)> cities)
    {
        if (string.IsNullOrWhiteSpace(agentId)) return;
        if (cities is null) return;

        var changed = false;
        lock (_gate)
        {
            foreach (var (cityId, name) in cities)
            {
                if (string.IsNullOrWhiteSpace(cityId)) continue;

                if (_cities.TryGetValue(cityId, out var existing))
                {
                    var updated = existing with { Name = name, AgentId = agentId };
                    if (!Equals(updated, existing))
                    {
                        _cities[cityId] = updated;
                        changed = true;
                    }
                }
                else
                {
                    _cities[cityId] = new CityWeatherState(cityId, name, agentId, TemperatureC: 0, LastUpdated: null);
                    changed = true;
                }
            }
        }

        if (changed) RaiseChanged();
    }

    public void ApplyWeatherUpdate(string cityId, string agentId, double tempC, DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(cityId)) return;
        if (string.IsNullOrWhiteSpace(agentId)) return;

        lock (_gate)
        {
            if (_cities.TryGetValue(cityId, out var existing))
            {
                _cities[cityId] = existing with { AgentId = agentId, TemperatureC = tempC, LastUpdated = timestamp };
            }
            else
            {
                // If a weather update arrives before registration, create a placeholder name using the cityId
                _cities[cityId] = new CityWeatherState(cityId, cityId, agentId, tempC, timestamp);
            }
        }
        RaiseChanged();
    }

    public RegistrySnapshot GetSnapshot()
    {
        lock (_gate)
        {
            // Materialize to stable snapshots
            var agents = _agents.Values
                .OrderBy(a => a.AgentId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var cities = _cities.Values
                .OrderBy(c => c.CityId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new RegistrySnapshot(agents, cities);
        }
    }

    private void RaiseChanged()
    {
        try
        {
            Changed?.Invoke();
        }
        catch
        {
            // Swallow subscriber exceptions to avoid destabilizing the registry.
        }
    }
}
