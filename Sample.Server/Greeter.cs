using Microsoft.AspNetCore.SignalR;
using Sample.Shared;

namespace Sample.Server;

public class Greeter(IHubContext<SampleHub, EventsToClient> hub, ILogger<Greeter> logger): BackgroundService {

    private readonly TimeSpan interval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            await hub.Clients.All.helloFromServer();
            logger.LogInformation("Sent hello to all clients");
            await Task.Delay(interval, stoppingToken);
        }
    }

}