using SignalRClientGenerator.Sample.Shared;

namespace SignalRClientGenerator.Sample.Client;

[GenerateSignalRClient(Incoming = [typeof(EventsToClient)], Outgoing = [typeof(EventsToServer)])]
internal partial class SampleClient;