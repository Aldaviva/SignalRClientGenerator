using SignalRClientGenerator.Sample.Shared;

namespace SignalRClientGenerator.Sample.Client;

[GenerateSignalRClient(incoming: [typeof(EventsToClient)], outgoing: [typeof(EventsToServer)])]
internal partial class SampleClient;