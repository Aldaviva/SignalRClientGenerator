namespace SignalRClientGenerator.Sample.Shared;

public interface EventsToServer: SuperEventsToServer {

    /// <summary>
    /// The client is saying hello to the server
    /// </summary>
    /// <param name="name">The client's name</param>
    /// <returns>Async valueless task</returns>
    Task helloFromClient(string name);

}

public interface SuperEventsToServer {

    Task superEventFromClient();

}