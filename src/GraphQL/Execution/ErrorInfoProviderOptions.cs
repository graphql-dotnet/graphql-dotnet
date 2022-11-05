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
        [Obsolete("Use ExposeExceptionDetails property instead")]
        public bool ExposeExceptionStackTrace
        {
            get => ExposeExceptionDetails;
            set
            {
                ExposeExceptionDetails = value;
                if (value)
                    ExposeExceptionDetailsMode = ExposeExceptionDetailsMode.Message;
            }
        }

        /// <summary>
        /// Specifies whether detailed exception information (exception types, stack traces, inner exceptions)
        /// should be serialized.
        /// </summary>
        public bool ExposeExceptionDetails { get; set; }

        /// <inheritdoc cref="Execution.ExposeExceptionDetailsMode"/>
        public ExposeExceptionDetailsMode ExposeExceptionDetailsMode { get; set; } = ExposeExceptionDetailsMode.Extensions;

        /// <summary>
        /// Specifies whether the extensions property, including by default the 'code',
        /// 'codes', 'data' and 'details' properties, should be serialized.
        /// </summary>
        public bool ExposeExtensions { get; set; } = true;

        /// <summary>
        /// Specifies whether the code of this error should be returned.
        /// For validation errors, also returns the <see cref="ValidationError.Number"/>.
        /// Not applicable when <see cref="ExposeExtensions"/> is <see langword="false"/>.
        /// </summary>
        public bool ExposeCode { get; set; } = true;

        /// <summary>
        /// Specifies whether the codes of this error and inner exceptions should be returned.
        /// Not applicable when <see cref="ExposeExtensions"/> is <see langword="false"/>.
        /// </summary>
        public bool ExposeCodes { get; set; } = true;

        /// <summary>
        /// Specifies whether data (typically from inner exceptions) should be returned.
        /// Not applicable when <see cref="ExposeExtensions"/> is <see langword="false"/>.
        /// </summary>
        public bool ExposeData { get; set; }
    }

    /// <summary>
    /// Mode to control location of exception details.
    /// </summary>
    public enum ExposeExceptionDetailsMode
    {
        /// <summary>
        /// Exception details are located along with exception message.
        /// </summary>
        Message,

        /// <summary>
        /// Exception details are located within "extensions.details" separately from exception message itself.
        /// </summary>
        Extensions
    }
}
