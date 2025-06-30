using Sample.Shared;
using SignalRClientGenerator;

namespace Sample.Client;

[GenerateSignalRClient(incoming: [typeof(EventsToClient)], outgoing: [typeof(EventsToServer)])]
public partial class SampleClient;