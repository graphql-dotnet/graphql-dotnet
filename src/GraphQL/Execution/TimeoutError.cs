namespace GraphQL.Execution;

/// <summary>
/// An error indicating that the execution of the GraphQL request has timed out due to
/// <see cref="ExecutionOptions.Timeout"/> expiring.
/// </summary>
public class TimeoutError : ExecutionError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutError"/> class with a default message.
    /// </summary>
    public TimeoutError()
        : this("The operation has timed out.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutError"/> class with a specified message.
    /// </summary>
    public TimeoutError(string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = "TIMEOUT";
    }
}
