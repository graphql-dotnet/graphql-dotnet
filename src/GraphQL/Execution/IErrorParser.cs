using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Execution
{
    /// <summary>
    /// Perpares <see cref="ExecutionError"/>s for serialization by the <see cref="IDocumentWriter"/>
    /// </summary>
    public interface IErrorParser
    {
        /// <summary>
        /// Parses an <see cref="ExecutionError"/> into a <see cref="ParsedError"/> class
        /// </summary>
        ParsedError Parse(ExecutionError executionError);
    }
}
