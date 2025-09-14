# Copilot instructions for this repo

This solution is a minimal agent-core demo built on Blazor Server + SignalR with shared contracts and an optional .NET Aspire AppHost. Use this guidance to extend features safely and idiomatically.

## Big picture
- Projects
  - `src/WebClientCore` (Blazor Server UI + SignalR Hub): hosts `ResourceHub` at `/hubs/resources` and renders live Agents/Cities from an in-memory registry.
  - `src/Agent` (Blazor Server host + background worker): connects as a SignalR client to the Core hub and periodically pushes data.
  - `src/Contracts` (shared records): `AgentInfo`, `CityRegistration`, `WeatherUpdate`.
  - `blazor-agent-core-signalr.AppHost` (optional): .NET Aspire app that runs Web + Agent.
  - `blazor-agent-core-signalr.ServiceDefaults`: OpenTelemetry, health checks, service discovery defaults.
- Data flow
  - Agent -> Core: `RegisterAgent`, `RegisterResources`, `PushWeatherUpdate`.
  - Core updates `IResourceRegistry` (in-memory) then raises `Changed`; UI (`Pages/Index.razor`) re-renders.
  - Core -> Agent: server->client messages (`RequestCityDetails`, `PerformSave`) via `IAgentBridge` correlation pattern; Agent replies with `ProvideCityDetails` and `SaveCompleted`.

## Key files and patterns
- `src/WebClientCore/Hubs/ResourceHub.cs`: Hub for agent calls; logs connect/disconnect; updates `IResourceRegistry`; forwards replies through `IAgentBridge`.
- `src/WebClientCore/Services/IResourceRegistry.cs` + `ResourceRegistry.cs`:
  - Thread-safe via a private lock, in-memory dictionaries for Agents/Cities; immutable records; emits `Changed` after mutations.
  - `GetSnapshot()` materializes sorted copies for stable UI rendering.
- `src/WebClientCore/Services/IAgentBridge.cs` + `AgentBridge.cs`:
  - Maintains `agentId -> connectionId` map.
  - Correlation-id + `TaskCompletionSource` with timeouts (10s default) to bridge UI requests to Agent responses.
  - Sends server->client via `IHubContext<ResourceHub>.Clients.Client(connectionId).SendAsync(...)`.
- `src/Agent/Services/WeatherPollingService.cs`:
  - `HubConnection` to `CoreHubUrl` (default `http://localhost:5100/hubs/resources`).
  - On (re)connect: `RegisterAgent` + `RegisterResources`; every ~5s sends `PushWeatherUpdate`.
  - Handles `RequestCityDetails` and `PerformSave`, then invokes back to Hub with results.
- `src/WebClientCore/Pages/Index.razor`: subscribes to `IResourceRegistry.Changed`, calls `IAgentBridge` methods to request details/save, displays modal.

## Extend safely (examples)
- New server->agent capability (request/reply):
  1) Add method to `IAgentBridge` + implement in `AgentBridge` using correlation-id + TCS timeout.
  2) In Agent, add `_connection.On<...>("YourMethod", ...)` and reply by invoking a new Hub method.
  3) In `ResourceHub`, add the reply method that completes the pending TCS via `IAgentBridge`.
- New agent->core updates: add Hub method in `ResourceHub`, invoke from Agent, update `IResourceRegistry` and raise `Changed`.
- Shared payloads: add records in `src/Contracts` referenced by both projects.

## Build / run
- Preferred (Aspire AppHost): run both services together
  - `dotnet run --project blazor-agent-core-signalr.AppHost`
- Alternative (individual projects):
  - Web: `dotnet run --project src/WebClientCore/WebClientCore.csproj`
  - Agent: `dotnet run --project src/Agent/Agent.csproj`
- Defaults:
  - Web port: `http://localhost:5100` (see `src/WebClientCore/Properties/launchSettings.json`). Hub path: `/hubs/resources`.
  - Agent hub URL: config key `CoreHubUrl` (defaults to `http://localhost:5100/hubs/resources`).

## Conventions
- IDs are case-insensitive; prefer replacing whole records (immutability) and then raising `Changed`.
- Always map the hub in WebClientCore `Program.cs`: `app.MapHub<ResourceHub>("/hubs/resources");`.
- Keep timeouts and `TaskCreationOptions.RunContinuationsAsynchronously` when adding bridged requests to avoid deadlocks.
- Use structured logging (already present in Hub/Bridge/Agent).

For an overview, see `README.md` (architecture and state diagrams).