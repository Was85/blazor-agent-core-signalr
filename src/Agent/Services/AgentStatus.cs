namespace Agent.Services;

public class AgentStatus
{
    public string ConnectionState { get; private set; } = "Disconnected";
    public string? LastMessage { get; private set; }
    public DateTimeOffset? LastUpdateAt { get; private set; }

    public void SetState(string state, string? message = null)
    {
        ConnectionState = state;
        LastMessage = message;
        LastUpdateAt = DateTimeOffset.UtcNow;
        Changed?.Invoke();
    }

    public event Action? Changed;
}