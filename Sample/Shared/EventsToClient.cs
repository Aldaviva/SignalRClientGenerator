namespace SignalRClientGenerator.Sample.Shared;

public interface EventsToClient: SuperEventsToClient {

    Task helloFromServer(DateTimeOffset currentTime);

}

public interface SuperEventsToClient {

    Task superEventFromServer();

}