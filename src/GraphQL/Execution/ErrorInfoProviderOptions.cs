using GraphQL.Validation;

namespace GraphQL.Execution
{
    /// <summary>
    /// Provides options to be used with <see cref="ErrorInfoProvider"/>
    /// </summary>
    public class ErrorInfoProviderOptions
    {
        /// <summary>
        /// Specifies whether stack traces should be serialized.
        /// </summary>
        public bool ExposeExceptionStackTrace { get; set; }

        /// <inheritdoc cref="Execution.ExposeExceptionStackTraceMode"/>
        public ExposeExceptionStackTraceMode ExposeExceptionStackTraceMode { get; set; }

        /// <summary>
        /// Specifies whether the extensions property, including by default the 'code', 'codes' and 'data' properties, should be serialized.
        /// </summary>
        public bool ExposeExtensions { get; set; } = true;

        /// <summary>
        /// Specifies whether the code of this error should be returned.
        /// For validation errors, also returns the <see cref="ValidationError.Number"/>.
        /// Not applicable when <see cref="ExposeExtensions"/> is <see langword="false"/>.
        /// </summary>
        public bool ExposeCode { get; set; } = true;

        /// <summary>
        /// Specifies whether the codes of this error and inner exceptions should be returned. Not applicable when <see cref="ExposeExtensions"/> is <see langword="false"/>.
        /// </summary>
        public bool ExposeCodes { get; set; } = true;

        /// <summary>
        /// Specifies whether data (typically from inner exceptions) should be returned. Not applicable when <see cref="ExposeExtensions"/> is <see langword="false"/>.
        /// </summary>
        public bool ExposeData { get; set; } = true;
    }

    /// <summary>
    /// Mode to control location of stack traces when <see cref="ErrorInfoProviderOptions.ExposeExceptionStackTrace"/>
    /// and <see cref="ErrorInfoProviderOptions.ExposeExtensions"/> are enabled.
    /// </summary>
    public enum ExposeExceptionStackTraceMode
    {
        /// <summary>
        /// Exception stack trace is located along with exception message.
        /// </summary>
        Message,

        /// <summary>
        /// Exception stack trace is located in "extensions.stacktrace" separately from exception message itself.
        /// </summary>
        Extensions
    }
}
