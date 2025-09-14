namespace WebClientCore.State;

public record RegistrySnapshot(
    IReadOnlyList<AgentState> Agents,
    IReadOnlyList<CityWeatherState> Cities);