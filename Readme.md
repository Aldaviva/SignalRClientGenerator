®️ SignalRClientGenerator
===

[![SignalRClientGenerator on NuGet Gallery](https://img.shields.io/nuget/v/SignalRClientGenerator?logo=nuget&color=success)](https://www.nuget.org/packages/SignalRClientGenerator)

*Automatically generate a strongly-typed .NET client for a SignalR server based on shared interfaces.*

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3" bullets="1." -->

1. [Background](#background)
1. [Problem](#problem)
1. [Solution](#solution)
    1. [Server side](#server-side)
    1. [Client side](#client-side)
1. [Sample](#sample)

<!-- /MarkdownTOC -->

## Background

[SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction) is a real-time message broker that resembles [Socket.IO](https://socket.io). It handles connection, reconnection, multiple transports (including WebSockets) and fallbacks, authentication and authorization, marshalling, queues and consumers, and RPC reply correlation semantics. It provides a server either in-process with ASP.NET Core apps or out-of-process hosted in Azure, and client libraries in .NET, Java, Javascript, and Swift.

Without SignalR, using raw WebSockets is very limited. There is no way to tell which clients are connected at any given time, handle disconnection events, send messages to only a subset of clients, frame messages that span multiple packets, serialize and deserialize types, or correlate a response to its request. You can write all that yourself, which is educational, but after doing it at most once you will want to use SignalR in the future.

## Problem

The SignalR server already allows type-safe events using [strongly-typed hubs](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs#strongly-typed-hubs). However, the client (including the .NET client, which is written in the same language as the server) does not have any built-in options for event type safety. All events received and sent from the client are weakly-typed using string names for methods and ad-hoc parameter and return types.

This means that if you change a method's name, parameters, or return type, these changes won't flow from the server to the client codebases. The out-of-date client code will continue to compile with its incorrect strings, and will either crash or silently ignore events at runtime. This is made worse by the fact that [important SignalR](https://github.com/Aldaviva/FreshPager/blob/ed0f1941326dbc1b6525539568dc124cbff21a26/FreshPager/Program.cs#L45) [marshalling errors](https://www.nuget.org/packages/Unfucked.DI) are logged as debug messages in the same class as lots of other unimportant debug messages, so they are very hard to spot and too verbose to leave on.

Even without the safety of strong types, having code completion makes development much simpler, faster, easier, and less annoying. APIs become more self-documenting, and an entire class of problems with incorrect or unknown signatures is eliminated.

## Solution

### Server side

> ℹ [Strongly-typed hubs](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs#strongly-typed-hubs) are already available built-in to ASP.NET Core SignalR, and they do not use this package. The steps to implement them are repeated here as a reminder, and because the client-side usage depends on the example shared code defined here. To see the novel client-side autogeneration, skip to the [Client side](#client-side) section.

To use strongly-typed server-side hubs, you define an interface for the events sent from server to client, and specify that interface as a generic type parameter for the `Hub<TClient>` that you subclass, as well as any `IHubContext<THub, TClient>` that you inject. To define the signatures of the events from the client to the server, you make your `Hub<TClient>` implement the interface of events from the client, and you implement those methods in your hub subclass.

The names, parameter types, and return types of both these sets of methods in the hub and client interfaces provide server-side type safety for the messages sent to and from the server.

#### Example

##### Share event definitions in interfaces
These interfaces should be extracted to a [shared library](https://github.com/Aldaviva/SignalRClientGenerator/tree/master/Sample/Shared) which is depended upon by both the server and client projects. These can also inherit from superinterfaces.

```cs
public interface EventsToClient {

    Task helloFromServer(DateTimeOffset currentTime);

}

public interface EventsToServer {

    Task helloFromClient(string name);

}
```

##### Receive messages on server
[Subclass](https://github.com/Aldaviva/SignalRClientGenerator/blob/master/Sample/Server/SampleHub.cs) the `Hub<TClient>` abstract class, parameterizing it with the interface of outgoing events, and also implement the interface of incoming events.
```cs
public class SampleHub(ILogger<SampleHub> logger): Hub<EventsToClient>, EventsToServer {

    public async Task helloFromClient(string name) {
        logger.LogInformation("{name} said hello", name);
    }

}
```

##### Send messages from server
[Inject](https://github.com/Aldaviva/SignalRClientGenerator/blob/master/Sample/Server/Greeter.cs) an `IHubContext<THub, TClient>` parameterized with the `Hub<TClient>` you subclassed and the outgoing events interface.
```cs
public class Greeter(IHubContext<SampleHub, EventsToClient> hub, ILogger<Greeter> logger): BackgroundService {

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            await hub.Clients.All.helloFromServer(DateTimeOffset.Now);
            logger.LogInformation("Sent hello to all clients");
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

}
```

### Client side

Normally, you would have to manually call `HubConnection.On` and pass the name of `helloFromServer` as a string, or at least use `nameof(EventsToClient.helloFromServer)` to allow only the name to be refactored.

By using a source generator, this package transforms the shared interfaces into a strongly-typed wrapper for your client-side `HubConnection`. Any time you add, change, or remove an event that is sent to or from the client, it will regenerate the strongly-typed client. This means that a new or changed parameter, changed type, or renamed method will immediately cause an obvious compiler error without a developer having to remember to do anything, instead of waiting to stealthily fail at runtime. It also makes it easier to develop clients because you don't have to manually retype every method signature, they're just provided for you with code completion.

#### Usage
1. Declare a dependency from your SignalR client [project](https://github.com/Aldaviva/SignalRClientGenerator/blob/master/Sample/Client/Client.csproj) to this generator package.
    ```ps1
    dotnet add package Microsoft.AspNetCore.SignalR.Client
    dotnet add package SignalRClientGenerator
    ```
1. Create a new empty partial class that will become the strongly-typed client.
    ```cs
    public partial class SampleClient;
    ```
1. [Annotate](https://github.com/Aldaviva/SignalRClientGenerator/blob/master/Sample/Client/SampleClient.cs) the class with `SignalRClientGenerator.GenerateSignalRClientAttribute`, specifying zero or more [interfaces that represent the incoming and outgoing events](#share-event-definitions-in-interfaces).
    ```cs
    [GenerateSignalRClient(incoming: [typeof(EventsToClient)], outgoing: [typeof(EventsToServer)])]
    public partial class SampleClient;
    ```
1. [Construct](https://github.com/Aldaviva/SignalRClientGenerator/blob/master/Sample/Client/Client.cs) a new instance of your client class, passing your `HubConnection` as an argument.
    ```cs
    await using HubConnection hub = new HubConnectionBuilder().WithUrl("http://localhost/events").Build();
    SampleClient client = new(hub);
    ```
1. Connect the hub to the server as with a normal SignalR client.
    ```cs
    await hub.StartAsync(cancellationToken);
    ```

##### Receive incoming events in client
Subscribe to the autogenerated event on your client class, instead of calling `hub.On("helloFromServer", (DateTimeOffset time) => {})`.
```cs
client.helloFromServer += async (sender, time) => Console.WriteLine($"It is currently {time}");
```

##### Send outgoing events from client
Call the autogenerated method on your client class, instead of calling `hub.InvokeAsync("helloFromClient", Environment.UserName)`.
```cs
await client.helloFromClient(Environment.UserName, cancellationToken);
```

## Sample
Check out the [sample server, client, and shared library](https://github.com/Aldaviva/SignalRClientGenerator/tree/master/Sample) projects in this repository for a complete example that compiles and runs.