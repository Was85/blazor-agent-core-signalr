using WebClientCore.Hubs;
using WebClientCore.Services;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();
builder.Services.AddFluentUIComponents();

builder.Services.AddSingleton<IResourceRegistry, ResourceRegistry>();
builder.Services.AddSingleton<IAgentBridge, AgentBridge>();

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
app.MapHub<ResourceHub>("/hubs/resources");
app.MapFallbackToPage("/_Host");

app.Run();
