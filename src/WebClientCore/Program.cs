using WebClientCore.Hubs;
using WebClientCore.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

builder.Services.AddSingleton<IResourceRegistry, ResourceRegistry>();

var app = builder.Build();

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
