using Microsoft.AspNetCore.SignalR;
using SignalRClientGenerator.Sample.Shared;

namespace SignalRClientGenerator.Sample.Server;

public class Greeter(IHubContext<SampleHub, EventsToClient> hub, ILogger<Greeter> logger): BackgroundService {

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            await hub.Clients.All.helloFromServer(DateTimeOffset.Now);
            logger.LogInformation("Sent hello to all clients");
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

}