using Microsoft.Extensions.Options;
using MyApi.AI.A2A;
using MyApi.AI.AgentRouter;
using MyApi.AI.Agents;
using MyApi.AI.Agents.Employee;
using MyApi.AI.Agents.Notification;
using MyApi.AI.Agents.Payroll;
using MyApi.AI.Agents.Reporting;
using MyApi.AI.Checkpointing;
using MyApi.AI.Harness;
using MyApi.AI.HITL;
using MyApi.AI.Memory;
using MyApi.AI.Middleware;
using MyApi.AI.Models;
using MyApi.AI.Observability;
using MyApi.AI.Orchestration;
using MyApi.AI.Plugins;
using MyApi.AI.Sessions;
using MyApi.AI.Streaming;
using MyApi.AI.Workflows.Definitions;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;
using MyApi.AI.Workflows.Steps;
using OpenAI;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MyApi.AI;

/// <summary>
/// Registers the enterprise multi-agent stack: harness, orchestration, router, A2A, workflows, agents, plugins.
/// </summary>
public static class AgentFrameworkServiceCollectionExtensions
{
    /// <summary>
    /// Adds the multi-agent AI platform with harness → orchestrator → agent router → workflow engine.
    /// </summary>
    public static IServiceCollection AddEmployeeAgentFramework(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AgentFrameworkOptions>(
            configuration.GetSection(AgentFrameworkOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddSingleton(CreateOpenAiClient);

        // AI middleware pipeline (ordered by IAgentMiddleware.Order)
        services.AddScoped<IAgentMiddleware, ExceptionMiddleware>();
        services.AddScoped<IAgentMiddleware, MyApi.AI.Middleware.AuthenticationMiddleware>();
        services.AddScoped<IAgentMiddleware, MyApi.AI.Middleware.AuthorizationMiddleware>();
        services.AddScoped<IAgentMiddleware, InputValidationMiddleware>();
        services.AddScoped<IAgentMiddleware, PromptLoggingMiddleware>();
        services.AddScoped<IAgentMiddleware, MemoryInjectionMiddleware>();
        services.AddScoped<IAgentMiddleware, ToolAuthorizationMiddleware>();
        services.AddScoped<IAgentMiddleware, TelemetryMiddleware>();
        services.AddScoped<IAgentMiddleware, CostTrackingMiddleware>();
        services.AddScoped<IAgentMiddleware, ResponseFilterMiddleware>();
        services.AddScoped<IAgentMiddlewarePipeline, AgentMiddlewarePipeline>();
        services.AddScoped<IToolInvocationPipeline, ToolInvocationPipeline>();

        // OpenTelemetry tracing for AI + ASP.NET + HTTP
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName: "MyApi.AI",
                serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(AiActivitySource.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter();
            });

        // Session store is singleton so conversations survive across scoped requests.
        services.AddSingleton<ISessionStore, InMemorySessionStore>();

        // Long-term memory (SQL) — independent from AgentSession.
        services.AddScoped<IMemoryStore, SqlMemoryStore>();
        services.AddScoped<IMemoryRetrievalService, MemoryRetrievalService>();
        services.AddScoped<IMemoryExtractionService, MemoryExtractionService>();

        // Domain plugins (own their service boundaries)
        services.AddScoped<EmployeePlugin>();
        services.AddScoped<PayrollPlugin>();
        services.AddScoped<ReportingPlugin>();
        services.AddScoped<NotificationPlugin>();

        // Domain agents — registered both as concrete + IDomainAgent for the router
        services.AddScoped<EmployeeAgent>();
        services.AddScoped<IEmployeeAgent>(sp => sp.GetRequiredService<EmployeeAgent>());
        services.AddScoped<IDomainAgent>(sp => sp.GetRequiredService<EmployeeAgent>());

        services.AddScoped<PayrollAgent>();
        services.AddScoped<IDomainAgent>(sp => sp.GetRequiredService<PayrollAgent>());

        services.AddScoped<ReportingAgent>();
        services.AddScoped<IDomainAgent>(sp => sp.GetRequiredService<ReportingAgent>());

        services.AddScoped<NotificationAgent>();
        services.AddScoped<IDomainAgent>(sp => sp.GetRequiredService<NotificationAgent>());

        // AG-UI streaming — SignalR is a transport only; domain code uses IStreamingService.
        services.AddSignalR();
        services.AddScoped<IStreamingTransport, SignalRStreamingTransport>();
        services.AddScoped<StreamingService>();
        services.AddScoped<IStreamingService>(sp => sp.GetRequiredService<StreamingService>());
        services.AddScoped<IEventPublisher>(sp => sp.GetRequiredService<StreamingService>());

        // Router + A2A (deferred router factory breaks agent ↔ communication cycles)
        services.AddScoped<IAgentRouter, AgentRouter.AgentRouter>();
        services.AddScoped<IAgentCommunication>(sp =>
            new AgentCommunication(
                () => sp.GetRequiredService<IAgentRouter>(),
                sp.GetRequiredService<IStreamingService>(),
                sp.GetRequiredService<ILogger<AgentCommunication>>()));

        // Workflow steps
        services.AddScoped<EmployeeLookupStep>();
        services.AddScoped<EmployeeSummaryStep>();
        services.AddScoped<ReportGenerationStep>();
        services.AddScoped<EmailReportStep>();
        services.AddScoped<DeleteEmployeeStep>();
        services.AddScoped<ValidateEmployeeDeleteStep>();
        services.AddScoped<RequestDeleteApprovalStep>();
        services.AddScoped<NotifyDeleteStep>();
        services.AddScoped<LoadEmployeeA2AStep>();
        services.AddScoped<LoadPayrollA2AStep>();
        services.AddScoped<GeneratePayrollReportA2AStep>();
        services.AddScoped<NotifyReportA2AStep>();
        services.AddScoped<RunSelectedAgentStep>();

        // HITL + durable checkpointing
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<ICheckpointStore, SqlCheckpointStore>();
        services.AddScoped<ICheckpointManager, CheckpointManager>();
        services.AddScoped<IWorkflowResumeService, WorkflowResumeService>();

        // Workflow definitions
        services.AddScoped<IWorkflowDefinition, EmployeeLookupWorkflow>();
        services.AddScoped<IWorkflowDefinition, EmployeeReportWorkflow>();
        services.AddScoped<IWorkflowDefinition, DeleteEmployeeWorkflow>();
        services.AddScoped<IWorkflowDefinition, PayrollReportWorkflow>();
        services.AddScoped<IWorkflowDefinition, GenerateEmployeePayrollReportWorkflow>();

        // Workflow engine
        services.AddScoped<IWorkflowExecutor, WorkflowExecutor>();
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();

        // Orchestration brain
        services.AddScoped<IIntentClassifier, IntentClassifier>();
        services.AddScoped<IWorkflowSelector, WorkflowSelector>();
        services.AddScoped<IAgentSelector, AgentSelector>();
        services.AddScoped<IOrchestrator, EnterpriseOrchestrator>();

        // Harness runtime — session/memory lifecycle; delegates decisions to orchestrator.
        services.AddScoped<IHarnessContextFactory, HarnessContextFactory>();
        services.AddScoped<IAgentHarness, EmployeeHarness>();
        services.AddScoped<EmployeeHarness>();
        services.AddScoped<ISessionManager, SessionManager>();

        return services;
    }

    private static OpenAIClient CreateOpenAiClient(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<AgentFrameworkOptions>>().Value;
        var apiKey = ResolveApiKey(options);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Set AgentFramework:ApiKey in configuration " +
                "or the OPENAI_API_KEY environment variable.");
        }

        return new OpenAIClient(apiKey);
    }

    private static string ResolveApiKey(AgentFrameworkOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return options.ApiKey;
        }

        return Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
    }
}
