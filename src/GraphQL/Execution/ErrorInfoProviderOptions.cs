namespace GraphQL.Execution
{
    /// <summary>
    /// Provides options to be used with <see cref="ErrorInfoProvider"/>
    /// </summary>
    public class ErrorInfoProviderOptions
    {
        /// <summary>
        /// Specifies whether stack traces should be serialized
        /// </summary>
        public bool ExposeExceptionStackTrace { get; set; }
        // public bool ExposeExtensions { get; set; }
    }
}
