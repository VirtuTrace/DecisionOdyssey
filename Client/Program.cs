using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Client;
using Client.JSWrappers;
using Client.Singletons;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

#if DEBUG
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("https://localhost:7280/") });
#else
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("https://3.141.31.200/") });
#endif
builder.Services.AddMudServices();
builder.Services.AddScoped<LocalStorageAccessor>();
builder.Services.AddScoped<FileHandler>();
builder.Services.AddScoped<EventAdder>();
builder.Services.AddScoped<BlobCreator>();
builder.Services.AddSingleton<ApplicationState>();
builder.Services.AddSingleton<HttpUtility>();

var host = builder.Build();

var localStorageAccessor = host.Services.GetRequiredService<LocalStorageAccessor>();
var applicationState = host.Services.GetRequiredService<ApplicationState>();
var httpUtility = host.Services.GetRequiredService<HttpUtility>();
var http = host.Services.GetRequiredService<HttpClient>();
await applicationState.InitializeAsync(localStorageAccessor);
httpUtility.Initialize(applicationState);
applicationState.Role = await httpUtility.GetUserRole(http);
if (applicationState.Role == "")
{
    Console.WriteLine("Clearing old credentials");
    await applicationState.ClearCredentials();
}

await host.RunAsync();