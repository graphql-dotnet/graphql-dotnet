namespace GraphQL.Execution;

/// <summary>
/// Defines the action to take when a timeout is reached during query execution.
/// </summary>
public enum TimeoutAction
{
    /// <summary>
    /// Return an <see cref="ExecutionResult"/> containing a <see cref="TimeoutError"/> when the timeout is reached.
    /// </summary>
    ReturnTimeoutError = 0,

    /// <summary>
    /// Throw a <see cref="TimeoutException"/> when the timeout is reached.
    /// </summary>
    ThrowTimeoutException = 1,
}
