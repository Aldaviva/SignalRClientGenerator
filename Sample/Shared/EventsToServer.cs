namespace SignalRClientGenerator.Sample.Shared;

public interface EventsToServer: SuperEventsToServer {

    Task helloFromClient();

}

public interface SuperEventsToServer {

    Task superEventFromClient();

}