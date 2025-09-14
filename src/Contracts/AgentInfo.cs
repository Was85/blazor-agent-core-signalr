namespace Contracts;

public record AgentInfo(
    string AgentId,
    string Name,
    DateTimeOffset LastSeen);