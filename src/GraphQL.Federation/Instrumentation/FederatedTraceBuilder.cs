using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Validation;
using Mdg.Engine.Proto;
using static Mdg.Engine.Proto.Trace.Types;

namespace GraphQL.Federation.Instrumentation
{
    /// <summary>
    /// Builds a proto tree structure with the tracing data based on the parent-child relationship
    /// among fields.
    /// </summary>
    public class FederatedTraceBuilder
    {
        private readonly PerfRecord[] _records;
        private readonly ExecutionErrors _errors;
        private readonly DateTime _start;

        /// <summary>
        /// Instantiates trace builder
        /// </summary>
        /// <param name="records">tracing data captured at parse/validate/execute steps</param>
        /// <param name="errors">errors if occurred during parse and validate step</param>
        /// <param name="start">process start time in UTC</param>
        public FederatedTraceBuilder(PerfRecord[] records, ExecutionErrors errors, DateTime start)
        {
            _records = records;
            _errors = errors;
            _start = start;
        }

        /// <summary>
        /// Initiate proto trace tree construction
        /// </summary>
        /// <returns></returns>
        private Trace BuildProtoTree()
        {
            var tree = new Trace();
            var operationStat = _records.Single(x => x.Category == "operation");
            tree.StartTime = _start.ToTimestamp();
            tree.EndTime = _start.AddMilliseconds(operationStat.Duration).ToTimestamp();
            tree.DurationNs = (ulong)operationStat.Duration * 1000000;
          
            var root = new ProtoTreeBuilder();
            AddRootErrors(root);
            AddFields(root);
            tree.Root = root.ToProto();
            return tree;
        }

        /// <summary>
        /// Adds root level errors (mostly paresing and validation error) to the proto tree
        /// </summary>
        /// <param name="tree">Proto tree to add error to</param>
        private void AddRootErrors(ProtoTreeBuilder tree)
        {
            if (_errors == null)
                return;
            foreach (var error in _errors)
            {
                if (error is SyntaxError syntaxError)
                {
                    tree.AddRootError(syntaxError);
                }
                else if (error is ValidationError validationError)
                {
                    tree.AddRootError(validationError);
                }
            }
        }

        /// <summary>
        /// Adds field level trace data to proto tree.
        /// </summary>
        /// <param name="tree">Proto tree to add error to</param>
        private void AddFields(ProtoTreeBuilder tree)
        {
            var fieldStats = _records.Where(x => x.Category == "federatedfield");

            foreach (var field in fieldStats)
            {
                tree.AddField(field);
            }
        }

        /// <summary>
        /// Serializes proto tree using protobuf and then converts serialized data into
        /// a base64 string.
        /// </summary>
        /// <returns></returns>
        public string ToProtoBase64() => Convert.ToBase64String(BuildProtoTree().ToByteArray());

        /// <summary>
        /// Private class used to build proto tree.
        /// </summary>
        private class ProtoTreeBuilder
        {
            private readonly Node _root;
            private readonly IDictionary<ResultPath, Node> _nodesByPath;
            public ProtoTreeBuilder()
            {
                _root = new Node();
                _nodesByPath = new Dictionary<ResultPath, Node>
                {
                    { ResultPath.ROOT_PATH, _root }
                };
            }

            /// <summary>
            /// Returns the root of the proto tree.
            /// </summary>
            /// <returns>returns <see cref="Trace.Types.Node"/> instance</returns>
            public Node ToProto() => _root;

            /// <summary>
            /// Adds error to the root node of the proto tree.
            /// </summary>
            /// <param name="error">error to add</param>
            public void AddRootError(ExecutionError error)
            {
                var root = GetOrCreateNode(ResultPath.ROOT_PATH);
                var protoError = new Error
                {
                    Message = error.Message
                };

                foreach (var location in error.Locations)
                {
                    var l = new Location
                    {
                        Column = (uint)location.Column,
                        Line = (uint)location.Line
                    };
                    protoError.Location.Add(l);
                }
                root.Error.Add(protoError);
            }

            /// <summary>
            /// Adds field trace record to proto tree. 
            /// </summary>
            /// <param name="record">Record to add</param>
            public void AddField(PerfRecord record)
            {
                var path = record.MetaField<IEnumerable<object>>("path").ToList();

                var p = ResultPath.FromList(path);
                var node = GetOrCreateNode(p);
                var type = record.MetaField<string>("type");
                var parentType = record.MetaField<string>("parentType");
                var responseName = record.MetaField<string>("responseName");
                var errors = record.MetaField<ExecutionErrors>("errors").ToArray();
       
                node.StartTime = (ulong)record.Start * 1000000;
                node.EndTime = (ulong)record.End * 1000000;
                node.ResponseName = responseName;
                node.ParentType = parentType;
                node.Type = type;
                if (errors == null)
                    return;
                foreach (var error in errors)
                {
                    var protoError = new Error
                    {
                        Message = error.Message
                    };

                    foreach (var location in error.Locations)
                    {
                        var tempLocation = new Location
                        {
                            Column = (uint)location.Column,
                            Line = (uint)location.Line
                        };

                        protoError.Location.Add(tempLocation);
                    }

                    node.Error.Add(protoError);
                }
            }

            /// <summary>
            /// Returns a <see cref="Node"/> represented by the given path if it has been calculated before.
            /// If not then it will create the node and its parents and store them and return the node represented
            /// by the path.
            /// </summary>
            /// <param name="path">Given <see cref="ResultPath"/></param>
            /// <returns><see cref="Node"/> instance</returns>
            private Node GetOrCreateNode(ResultPath path)
            {
                if (_nodesByPath.TryGetValue(path, out var current))
                {
                    return current;
                }
                var pathSegments = path.ToList();
                int currentSegmentIndex = pathSegments.Count();
                while (current == null)
                {
                    if (currentSegmentIndex <= 0)
                    {
                        // this should never occur.
                        throw new IndexOutOfRangeException("root path missing?");
                    }
                    currentSegmentIndex--;
                    var currentPath = ResultPath.FromList(pathSegments.Take(currentSegmentIndex).ToList());
                    current = _nodesByPath.TryGetValue(currentPath, out var tCurrent) ? tCurrent : null;
                }

                for (; currentSegmentIndex < pathSegments.Count(); currentSegmentIndex++)
                {
                    var parent = current;
                    var childPath = ResultPath.FromList(pathSegments.Take(currentSegmentIndex).ToList());
                    object childSegment = pathSegments.ElementAtOrDefault(currentSegmentIndex);

                    var child = new Node();
                    if (childSegment is int)
                    {
                        child.Index = Convert.ToUInt32(childSegment);
                    }

                    if (!_nodesByPath.ContainsKey(childPath))
                    {
                        _nodesByPath.Add(childPath, child);
                        current = child;
                        parent.Child.Add(child);
                    }
                }
                return current;
            }
        }
    }
}
