using Microsoft.AspNetCore.SignalR.Client;
using SignalRClientGenerator.Sample.Client;
using Unfucked;
using Timer = System.Timers.Timer;

using CancellationTokenSource cts = new CancellationTokenSource().CancelOnCtrlC();
await using HubConnection     hub = new HubConnectionBuilder().WithUrl("http://localhost:7447/events").Build();

hub.Closed += async _ => Console.WriteLine("Disconnected");

SampleClient client = new(hub);
client.helloFromServer      += async _ => Console.WriteLine("Hello from server");
client.superEventFromServer += async _ => Console.WriteLine("Super event from server");

Console.WriteLine("Connecting");
await hub.StartAsync(cts.Token);
Console.WriteLine("Connected");

await client.superEventFromClient(CancellationToken.None);

using Timer timer = new(TimeSpan.FromSeconds(1)) { Enabled = true, AutoReset = true };
timer.Elapsed += async (_, _) => {
    await client.helloFromClient(cts.Token);
    Console.WriteLine("Sent hello to server");
};

await cts.Token.Wait();