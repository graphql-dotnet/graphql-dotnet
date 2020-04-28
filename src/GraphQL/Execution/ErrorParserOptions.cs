using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Execution
{
    /// <summary>
    /// Provides options to be used with <see cref="ErrorParser"/>
    /// </summary>
    public class ErrorParserOptions
    {
        /// <summary>
        /// Specifies whether stack traces should be serialized
        /// </summary>
        public bool ExposeExceptions { get; set; }
        // public bool ExposeExtensions { get; set; }
    }
}
