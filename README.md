# Blazor Agent-Core SignalR

Two Blazor Server apps demonstrating an Agent/Core architecture with SignalR:
- WebClientCore: central registry that receives agent registrations and live resource updates (cities/temperatures) and displays them.
- Agent: background worker inside a Blazor Server app that mocks a weather API for 3 cities and pushes temperature changes to WebClientCore via SignalR.

## Projects

- `src/Contracts` — shared DTOs (`AgentInfo`, `CityRegistration`, `WeatherUpdate`)
- `src/WebClientCore` — SignalR hub `/hubs/resources`, in-memory registry, Blazor UI
- `src/Agent` — connects to the hub, registers, and periodically pushes updates

## Run locally (requires .NET 8 SDK)

Open two terminals:

```bash
# Terminal 1: start the core
dotnet run -p src/WebClientCore

# Terminal 2: start the agent
dotnet run -p src/Agent
```

Open the UI: http://localhost:5100

You should see:
- Agents list with one agent (your machine)
- Cities list with New York, London, Tokyo
- Temperatures updating every ~5 seconds

Ports:
- WebClientCore: 5100
- Agent: 5200

## Configuration

Agent reads the hub URL and identity from configuration (env vars or appsettings):

- `CoreHubUrl` (default: `http://localhost:5100/hubs/resources`)
- `Agent:AgentId` (default: `agent-{MachineName}`)
- `Agent:Name` (default: `Agent on {MachineName}`)

## Architecture

See docs/architecture.md for Mermaid diagrams.

## Notes

- UI uses the server-side in-memory registry; SignalR is used between Agent -> Core only.
- For production, consider persistence, authentication (API keys), retries/backoff, and health checks.
