using SignalRClientGenerator.Sample.Server;
using Unfucked;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddHostedService<Greeter>();
builder.Logging.AmplifyMessageLevels(options => options.Amplify("Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcher", LogLevel.Warning, 2, 3, 5, 11, 13, 14, 15, 19, 21, 22, 23, 24));

await using WebApplication webapp = builder.Build();
webapp.MapHub<SampleHub>("/events");
await webapp.RunAsync();