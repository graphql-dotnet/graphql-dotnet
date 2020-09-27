using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using GraphQL.Instrumentation;
using mdg.engine.proto;
using Newtonsoft.Json;

namespace GraphQL.ApolloStudio
{
    /// <summary>
    /// Converts GraphQL execution results to Apollo Studio protobuf traces
    /// </summary>
    public class MetricsToTraceConverter
    {
        /// <summary>
        /// Create a protobuf trace instance from a GraphQL execution result
        /// </summary>
        /// <param name="result">The execution result from the GraphQL document executor</param>
        /// <param name="start">The time the request was initiated</param>
        /// <returns></returns>
        public Trace CreateTrace(ExecutionResult result, DateTime start)
        {
            var trace = (result?.Extensions != null && result.Extensions.ContainsKey("tracing") ? (ApolloTrace)result.Extensions["tracing"] : null)
                        ?? (result?.Perf != null ? ApolloTracingExtensions.CreateTrace(result.Perf, start) : null);

            var rootTrace = trace?.Execution.Resolvers?.SingleOrDefault(x => x.Path.Count == 1);
            if (rootTrace == null && result?.Errors == null)
                return null;

            var rootErrors = result.Errors?.Where(x => x.Path != null && x.Path.Count() == 1).ToArray();

            var rootNode = rootTrace != null && trace.Execution.Resolvers != null
                ? CreateNodes(rootTrace.Path, CreateNodeForResolver(rootTrace, rootErrors), GetSubResolvers(rootTrace.Path, trace.Execution.Resolvers.ToArray()), GetSubErrors(rootTrace.Path, result.Errors?.ToArray()))
                : new Trace.Node();

            if (rootTrace == null && result.Errors != null)
            {
                foreach (var executionError in result.Errors)
                    rootNode.Errors.Add(CreateTraceError(executionError));
            }

            return new Trace
            {
                StartTime = trace?.StartTime ?? DateTime.Now,
                EndTime = trace?.EndTime ?? DateTime.Now,
                DurationNs = (ulong)(trace?.Duration ?? 0),
                http = new Trace.Http { method = Trace.Http.Method.Post, StatusCode = result.Errors?.Any() == true ? (uint)HttpStatusCode.BadRequest : (uint)HttpStatusCode.OK },
                Root = rootNode
            };
        }

        private static Trace.Node CreateNodeForResolver(ApolloTrace.ResolverTrace resolver, ExecutionError[] executionErrors)
        {
            var node = new Trace.Node
            {
                ResponseName = resolver.FieldName,
                Type = resolver.ReturnType,
                StartTime = (ulong)resolver.StartOffset,
                EndTime = (ulong)(resolver.StartOffset + resolver.Duration),
                ParentType = resolver.ParentType
            };

            if (executionErrors != null)
            {
                foreach (var executionError in executionErrors)
                    node.Errors.Add(CreateTraceError(executionError));
            }

            return node;
        }

        private static Trace.Error CreateTraceError(ExecutionError executionError)
        {
            var error = new Trace.Error
            {
                Json = JsonConvert.SerializeObject(executionError),
                Message = executionError.Message
            };
            if (executionError.Locations != null)
                error.Locations.AddRange(executionError.Locations.Select(x => new Trace.Location { Column = (uint)x.Column, Line = (uint)x.Line }));
            return error;
        }

        private static ApolloTrace.ResolverTrace[] GetSubResolvers(IReadOnlyCollection<object> path, ApolloTrace.ResolverTrace[] resolvers) =>
            resolvers
                .Where(x => x.Path.Count > path.Count && x.Path.Take(path.Count).SequenceEqual(path))
                .ToArray();

        private static ExecutionError[] GetSubErrors(IReadOnlyCollection<object> path, ExecutionError[] errors) =>
            errors
                ?.Where(x => x.Path.Count() > path.Count && x.Path.Take(path.Count).SequenceEqual(path))
                .ToArray();

        private static Trace.Node CreateNodes(IReadOnlyCollection<object> path, Trace.Node node, ApolloTrace.ResolverTrace[] resolvers, ExecutionError[] executionErrors)
        {
            bool isArray = node.Type.StartsWith("[") && node.Type.TrimEnd('!').EndsWith("]");
            if (isArray)
            {
                foreach (int index in resolvers.Where(x => x.Path.Count == path.Count + 2).Select(x => (int)x.Path[x.Path.Count - 2]).Distinct().OrderBy(x => x))
                {
                    var subPath = path.Concat(new object[] { index }).ToList();

                    node.Childs.Add(CreateNodes(subPath,
                        new Trace.Node
                        {
                            Index = (uint)index,
                            ParentType = node.Type,
                            Type = node.Type.TrimStart('[').TrimEnd('!').TrimEnd(']')
                        }, GetSubResolvers(subPath, resolvers), GetSubErrors(subPath, executionErrors)));
                }
            }
            else
            {
                foreach (var resolver in resolvers.Where(x => x.Path.Count == path.Count + 1 && x.Path.Take(path.Count).SequenceEqual(path)))
                {
                    var errors = executionErrors?.Where(x => x.Path.SequenceEqual(resolver.Path)).ToArray();
                    node.Childs.Add(CreateNodes(resolver.Path, CreateNodeForResolver(resolver, errors), GetSubResolvers(resolver.Path, resolvers), GetSubErrors(resolver.Path, executionErrors)));
                }
            }

            return node;
        }

    }
}
