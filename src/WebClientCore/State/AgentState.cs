using System;

namespace WebClientCore.State;

public record AgentState(
    string AgentId,
    string Name,
    DateTimeOffset LastSeen);