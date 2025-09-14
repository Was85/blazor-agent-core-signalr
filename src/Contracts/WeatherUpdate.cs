namespace Contracts;

public record WeatherUpdate(
    string CityId,
    string AgentId,
    double TemperatureC,
    DateTimeOffset Timestamp);