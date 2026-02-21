namespace SignalRClientGenerator.Sample.Shared;

public interface EventsToClient: SuperEventsToClient {

    /// <summary>
    /// The server is saying hello to the client
    /// </summary>
    /// <remarks>Some remarks</remarks>
    /// <param name="currentTime">The current time</param>
    /// <returns>Async valueless task</returns>
    /// <seealso cref="EventsToServer.helloFromClient"/>
    Task helloFromServer(DateTimeOffset currentTime);

}

public interface SuperEventsToClient {

    Task superEventFromServer();

}