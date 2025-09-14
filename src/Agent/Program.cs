using Agent.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configuration defaults
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["CoreHubUrl"] = "http://localhost:5100/hubs/resources",
    ["Agent:AgentId"] = $"agent-{Environment.MachineName}".ToLowerInvariant(),
    ["Agent:Name"] = $"Agent on {Environment.MachineName}"
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Status shared to UI
builder.Services.AddSingleton<AgentStatus>();

// Worker that connects to Core and pushes updates
builder.Services.AddHostedService<WeatherPollingService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();