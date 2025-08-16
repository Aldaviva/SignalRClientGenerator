namespace SignalRClientGenerator.Sample.Shared;

public interface EventsToClient: SuperEventsToClient {

    Task helloFromServer();

}

public interface SuperEventsToClient {

    Task superEventFromServer();

}