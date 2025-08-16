using Microsoft.AspNetCore.SignalR;
using SignalRClientGenerator.Sample.Shared;

namespace SignalRClientGenerator.Sample.Server;

public class SampleHub(ILogger<SampleHub> logger): Hub<EventsToClient>, EventsToServer {

    public async Task helloFromClient() {
        logger.LogInformation("Client said hello");
    }

    public override Task OnConnectedAsync() {
        logger.LogDebug("Client connected");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception) {
        logger.LogDebug("Client disconnected");
        return base.OnDisconnectedAsync(exception);
    }

}