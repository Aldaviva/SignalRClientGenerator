namespace SignalRClientGenerator.Sample.Shared;

public interface EventsToServer: SuperEventsToServer {

    Task helloFromClient(string name);

}

public interface SuperEventsToServer {

    Task superEventFromClient();

}