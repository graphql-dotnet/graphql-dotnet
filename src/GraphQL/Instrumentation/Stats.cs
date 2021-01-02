using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQL.Validation;
using AstField = GraphQL.Language.AST.Field;

namespace GraphQL.Instrumentation
{
    [Obsolete]
    public class FieldStat
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }

        // TODO: switch this to a histogram
        public double Latency { get; set; }

        public void AddLatency(double duration)
        {
            Latency += duration;
        }
    }

    [Obsolete]
    public class TypeStat
    {
        private readonly LightweightCache<string, FieldStat> _fields =
            new LightweightCache<string, FieldStat>(fieldName => new FieldStat { Name = fieldName });

        public string Name { get; set; }

        public FieldStat[] Fields
        {
            get => _fields.GetAll();
            set
            {
                _fields.Clear();

                value.Apply(f => _fields[f.Name] = f);
            }
        }

        public FieldStat this[string fieldName] => _fields[fieldName];
    }

    [Obsolete]
    public class StatsPerSignature
    {
        public TypeStat[] PerType { get; set; }
    }

    [Obsolete]
    public class Field
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
    }

    [Obsolete]
    public class Type
    {
        public string Name { get; set; }
        public Field[] Fields { get; set; }
    }

    [Obsolete]
    public class StatsReport
    {
        public StatsReport()
        {
            PerSignature = new Dictionary<string, StatsPerSignature>();
        }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public double Duration { get; set; }

        public Dictionary<string, StatsPerSignature> PerSignature { get; set; }
        public Type[] Types { get; set; }

        public static StatsReport From(ISchema schema, Operation operation, PerfRecord[] records, DateTime start)
        {
            var operationStat = records.Single(x => string.Equals(x.Category, "operation"));

            var report = new StatsReport
            {
                Start = start,
                End = start.AddMilliseconds(operationStat.Duration),
                Duration = operationStat.Duration,
                Types = TypesFromSchema(schema)
            };

            var perField = new LightweightCache<string, TypeStat>(type => new TypeStat { Name = type });

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

            report.PerSignature[operationName] = new StatsPerSignature { PerType = perField.GetAll() };

            return report;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Will be removed in v4")]
        public static Type[] TypesFromSchema(ISchema schema)
        {
            return null;
        }
    }
}
