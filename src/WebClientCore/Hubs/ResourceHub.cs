using Contracts;
using Microsoft.AspNetCore.SignalR;
using WebClientCore.Services;

namespace WebClientCore.Hubs;

public class ResourceHub(IResourceRegistry registry, ILogger<ResourceHub> logger) : Hub
{
    public override Task OnConnectedAsync()
    {
        logger.LogInformation("Agent connection established: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Agent disconnected: {ConnectionId}. Exception: {Message}", Context.ConnectionId, exception?.Message);
        return base.OnDisconnectedAsync(exception);
    }

    public Task RegisterAgent(AgentInfo agent)
    {
        logger.LogInformation("RegisterAgent: {AgentId} - {Name}", agent.AgentId, agent.Name);
        registry.UpsertAgent(agent.AgentId, agent.Name, agent.LastSeen);
        return Task.CompletedTask;
    }

    public Task RegisterResources(IEnumerable<CityRegistration> cities, string agentId)
    {
        var cityList = cities.ToList();
        logger.LogInformation("RegisterResources from {AgentId}: {Count} cities", agentId, cityList.Count);
        registry.UpsertResources(agentId, cityList.Select(c => (c.CityId, c.Name)));
        return Task.CompletedTask;
    }

    public Task PushWeatherUpdate(WeatherUpdate update)
    {
        registry.ApplyWeatherUpdate(update.CityId, update.AgentId, update.TemperatureC, update.Timestamp);
        return Task.CompletedTask;
    }
}