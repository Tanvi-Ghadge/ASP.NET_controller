using System.Diagnostics;

namespace MyApi.AI.Observability;

/// <summary>
/// Central <see cref="ActivitySource"/> for AI platform spans (Harness, Middleware, Workflow, Agent, Plugin, Tool).
/// </summary>
public static class AiActivitySource
{
    public const string Name = "MyApi.AI";

    public static readonly ActivitySource Source = new(Name, "1.0.0");

    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal) =>
        Source.StartActivity(name, kind);
}
