#if NET5_0_OR_GREATER

using System.Diagnostics;
using GraphQL.Execution;
using GraphQL.Validation;

namespace GraphQL.Instrumentation;

internal class TelemetryListener : DocumentExecutionListenerBase
{
    private readonly Activity _activity;

    public TelemetryListener(Activity activity)
    {
        _activity = activity;
    }

    public override Task AfterValidationAsync(IExecutionContext context, IValidationResult validationResult)
    {
        var operationType = context.Operation.Operation.ToString().ToLowerInvariant();
        _activity.SetTag("graphql.operation.type", operationType);
        var operationName = context.Operation.Name?.StringValue;
        _activity.SetTag("graphql.operation.name", operationName);
        _activity.DisplayName = operationName == null ? operationType : $"{operationType} {operationName}";
        return Task.CompletedTask;
    }
}

#endif
