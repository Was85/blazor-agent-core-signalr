namespace WebClientCore.State;

public record CityWeatherState(
    string CityId,
    string Name,
    string AgentId,
    double TemperatureC,
    DateTimeOffset? LastUpdated);