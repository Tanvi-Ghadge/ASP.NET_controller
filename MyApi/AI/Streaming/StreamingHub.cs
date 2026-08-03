namespace MyApi.AI.Streaming;

/// <summary>
/// Marker documenting the Streaming hub surface.
/// The live SignalR hub type is <see cref="AgentHub"/> (mapped at <c>/hubs/agent</c>).
/// Kept so the Streaming folder contract remains discoverable as StreamingHub.
/// </summary>
public static class StreamingHub
{
    /// <summary>Recommended client hub path.</summary>
    public const string HubPath = "/hubs/agent";

    /// <summary>Client method name invoked by <see cref="SignalRStreamingTransport"/>.</summary>
    public const string ClientEventMethod = SignalRStreamingTransport.ClientMethod;
}
