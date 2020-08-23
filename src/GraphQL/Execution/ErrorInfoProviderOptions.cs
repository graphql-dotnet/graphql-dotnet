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

        /// <summary>
        /// Specifies whether the extensions property, including by default the 'code', 'codes' and 'data' properties, should be serialized.
        /// </summary>
        public bool ExposeExtensions { get; set; }

        /// <summary>
        /// Specifies whether the code of this error should be returned. Not applicable when <see cref="ExposeExtensions"/> is false.
        /// </summary>
        public bool ExposeCode { get; set; }

        /// <summary>
        /// Specifies whether the codes of this error and inner exceptions should be returned. Not applicable when <see cref="ExposeExtensions"/> is false.
        /// </summary>
        public bool ExposeCodes { get; set; }

        /// <summary>
        /// Specifies whether data (typically from inner exceptions) should be returned. Not applicable when <see cref="ExposeExtensions"/> is false.
        /// </summary>
        public bool ExposeData { get; set; }
    }
}
