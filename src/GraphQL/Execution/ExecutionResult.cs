using System;
using System.Collections.Generic;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using Newtonsoft.Json;

namespace GraphQL
{
    [JsonConverter(typeof(ExecutionResultJsonConverter))]
    public class ExecutionResult
    {
        public object Data { get; set; }

        public ExecutionErrors Errors { get; set; }

        public string Query { get; set; }

        public Document Document { get; set; }

        public Operation Operation { get; set; }

        public PerfRecord[] Perf { get; set; }

        public bool ExposeExceptions { get; set; }

        public Dictionary<string, object> Extensions { get; set; }

        public ExecutionResult()
        {
        }

        protected ExecutionResult(ExecutionResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            Data = result.Data;
            Errors = result.Errors;
            Query = result.Query;
            Operation = result.Operation;
            Document = result.Document;
            Perf = result.Perf;
            ExposeExceptions = result.ExposeExceptions;
            Extensions = result.Extensions;
        }
    }
}
