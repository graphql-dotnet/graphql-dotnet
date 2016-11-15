using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQL.Validation;
using AstField = GraphQL.Language.AST.Field;

namespace GraphQL.Instrumentation
{
    [DebuggerDisplay("Type={Category} Subject={Subject} Duration={Duration}")]
    public class PerfRecord
    {
        public PerfRecord()
        {
        }

        public PerfRecord(string category, string subject, long start, Dictionary<string, object> metadata = null)
        {
            Category = category;
            Subject = subject;
            Start = start;
            Metadata = metadata;
        }

        public void MarkEnd(long end)
        {
            End = end;
        }

        public string Category { get; set; }

        public string Subject { get; set; }

        public Dictionary<string, object> Metadata { get; set; }

        public long Start { get; set; }

        public long End { get; set; }

        public long Duration => End - Start;

        public T MetaField<T>(string key)
        {
            object value;

            if (Metadata.TryGetValue(key, out value))
            {
                return (T)value;
            }

            return default(T);
        }
    }

    public class FieldStat
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }

        // TODO: switch this to a histogram
        public long Latency { get; set; }

        public void AddLatency(long duration)
        {
            Latency += duration;
        }
    }

    public class TypeStat
    {
        private readonly LightweightCache<string, FieldStat> _fields =
            new LightweightCache<string, FieldStat>(fieldName => new FieldStat {Name = fieldName});

        public string Name { get; set; }

        public FieldStat[] Fields
        {
            get { return _fields.GetAll(); }
            set
            {
                _fields.Clear();

                value.Apply(f =>
                {
                    _fields[f.Name] = f;
                });
            }
        }

        public FieldStat this[string fieldName] => _fields[fieldName];
    }

    public class StatsPerSignature
    {
        public TypeStat[] PerType { get; set; }
    }

    public class Field
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
    }

    public class Type
    {
        public string Name { get; set; }
        public Field[] Fields { get; set; }
    }

    public class StatsReport
    {
        public StatsReport()
        {
            PerSignature = new Dictionary<string, StatsPerSignature>();
        }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public long Duration { get; set; }

        public Dictionary<string, StatsPerSignature> PerSignature { get; set; }
        public Type[] Types { get; set; }

        public static StatsReport From(ISchema schema, Operation operation, PerfRecord[] records, DateTime start)
        {
            var operationStat = records.Single(x => string.Equals(x.Category, "operation"));

            var report = new StatsReport();
            report.Start = start;
            report.End = start.AddMilliseconds(operationStat.Duration);
            report.Duration = operationStat.Duration;
            report.Types = TypesFromSchema(schema);

            var perField = new LightweightCache<string, TypeStat>(type => new TypeStat {Name = type});

            var typeInfo = new TypeInfo(schema);

            var fieldVisitor = new EnterLeaveListener(_ =>
            {
                _.Match<AstField>(f =>
                {
                    var parent = typeInfo.GetParentType().GetNamedType();
                    var parentType = parent.Name;
                    var fieldName = f.Name;

                    perField[parentType][fieldName].ReturnType = SchemaPrinter.ResolveName(typeInfo.GetLastType());
                });
            });

            new BasicVisitor(typeInfo, fieldVisitor).Visit(operation);

            var queryResolvers = records.Where(x => string.Equals(x.Category, "field")).ToList();

            queryResolvers.Apply(resolver =>
            {
                var typeName = resolver.MetaField<string>("typeName");
                var fieldName = resolver.MetaField<string>("fieldName");

                perField[typeName][fieldName].AddLatency(resolver.Duration);
            });

            var operationName = operation.Name ?? "Anonymous";

            report.PerSignature[operationName] = new StatsPerSignature {PerType = perField.GetAll()};

            return report;
        }

        public static Type[] TypesFromSchema(ISchema schema)
        {
            return null;
        }
    }
}
