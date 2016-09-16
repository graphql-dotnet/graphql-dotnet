using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public class SchemaPrinter : IDisposable
    {
        private ISchema _schema;

        private readonly List<string> _scalars = new List<string>(
            new[]
            {
                // core types
                "String",
                "Boolean",
                "Int",
                "Float",
                "ID",
                // added types
                "Date",
                "Decimal"
            });

        public SchemaPrinter(ISchema schema, IEnumerable<string> customScalars = null)
        {
            _schema = schema;

            if (customScalars != null)
            {
                _scalars.Fill(customScalars);
            }
        }

        private ISchema Schema => _schema;

        public string Print()
        {
            return PrintFilteredSchema(IsDefinedType);
        }

        public string PrintIntrospectionSchema()
        {
            return PrintFilteredSchema(IsIntrospectionType);
        }

        public string PrintFilteredSchema(Func<string, bool> typeFilter)
        {
            return string.Join(Environment.NewLine + Environment.NewLine,
                Schema.AllTypes
                    .Where(t => typeFilter(t.Name))
                    .Select(PrintType)
                    .OrderBy(x => x))
                   + Environment.NewLine;
        }

        public bool IsDefinedType(string typeName)
        {
            return !IsIntrospectionType(typeName) && !IsBuiltInScalar(typeName);
        }

        public bool IsIntrospectionType(string typeName)
        {
            return typeName.StartsWith("__");
        }

        public bool IsBuiltInScalar(string typeName)
        {
            return _scalars.Contains(typeName);
        }

        public string PrintType(IGraphType type)
        {
            if (type is EnumerationGraphType)
            {
                return PrintEnum((EnumerationGraphType)type);
            }

            if (type is ScalarGraphType)
            {
                return PrintScalar((ScalarGraphType)type);
            }

            if (type is IObjectGraphType)
            {
                return PrintObject((IObjectGraphType)type);
            }

            if (type is IInterfaceGraphType)
            {
                return PrintInterface((IInterfaceGraphType)type);
            }

            if (type is UnionGraphType)
            {
                return PrintUnion((UnionGraphType)type);
            }

            if (!(type is InputObjectGraphType))
            {
                throw new InvalidOperationException("Unknown GraphType {0}".ToFormat(type.GetType().Name));
            }

            return PrintInputObject((InputObjectGraphType)type);
        }

        public string PrintScalar(ScalarGraphType type)
        {
            return "scalar {0}".ToFormat(type.Name);
        }

        public string PrintObject(IObjectGraphType type)
        {
            var interfaces = type.Interfaces.Select(x => Schema.FindType(x).Name).ToList();
            var implementedInterfaces = interfaces.Any()
                ? " implements {0}".ToFormat(string.Join(", ", interfaces))
                : "";

            return "type {1}{2} {{{0}{3}{0}}}".ToFormat(Environment.NewLine, type.Name, implementedInterfaces, PrintFields(type));
        }

        public string PrintInterface(IInterfaceGraphType type)
        {
            return "interface {1} {{{0}{2}{0}}}".ToFormat(Environment.NewLine, type.Name, PrintFields(type));
        }

        public string PrintUnion(UnionGraphType type)
        {
            var possibleTypes = string.Join(" | ", type.PossibleTypes.Select(x => x.Name));
            return "union {0} = {1}".ToFormat(type.Name, possibleTypes);
        }

        public string PrintEnum(EnumerationGraphType type)
        {
            var values = string.Join(Environment.NewLine, type.Values.Select(x => "  " + x.Name));
            return "enum {1} {{{0}{2}{0}}}".ToFormat(Environment.NewLine, type.Name, values);
        }

        public string PrintInputObject(InputObjectGraphType type)
        {
            var fields = type.Fields.Select(x => "  " + PrintInputValue(x));
            return "input {1} {{{0}{2}{0}}}".ToFormat(Environment.NewLine, type.Name, string.Join(Environment.NewLine, fields));
        }

        public string PrintFields(IComplexGraphType type)
        {
            var fields = type?.Fields
                .Select(x =>
                new
                {
                    x.Name,
                    Type = ResolveName(Schema.FindType(x.Type)),
                    Args = PrintArgs(x)
                }).ToList();

            return string.Join(Environment.NewLine, fields?.Select(f => "  {0}{1}: {2}".ToFormat(f.Name, f.Args, f.Type)));
        }

        public string PrintArgs(FieldType field)
        {
            if (field.Arguments == null || !field.Arguments.Any())
            {
                return string.Empty;
            }

            return "({0})".ToFormat(string.Join(", ", field.Arguments.Select(PrintInputValue)));
        }

        public string PrintInputValue(FieldType argument)
        {
            var argumentType = Schema.FindType(argument.Type);
            var desc = "{0}: {1}".ToFormat(argument.Name, ResolveName(argumentType));

            if (argument.DefaultValue != null)
            {
                desc += " = ".ToFormat(FormatDefaultValue(argument.DefaultValue));
            }

            return desc;
        }

        public string PrintInputValue(QueryArgument argument)
        {
            var argumentType = Schema.FindType(argument.Type);
            var desc = "{0}: {1}".ToFormat(argument.Name, ResolveName(argumentType));

            if (argument.DefaultValue != null)
            {
                desc += " = {0}".ToFormat(FormatDefaultValue(argument.DefaultValue));
            }

            return desc;
        }

        public string FormatDefaultValue(object value)
        {
            if (value is string)
            {
                return "\"{0}\"".ToFormat(value);
            }

            if (value is bool)
            {
                return value.ToString().ToLower();
            }

            return "{0}".ToFormat(value);
        }

        public string ResolveName(IGraphType type)
        {
            if (type is NonNullGraphType)
            {
                var nullable = (NonNullGraphType)type;
                return "{0}!".ToFormat(ResolveName(Schema.FindType(nullable.Type)));
            }

            if (type is ListGraphType)
            {
                var list = (ListGraphType)type;
                return "[{0}]".ToFormat(ResolveName(Schema.FindType(list.Type)));
            }

            return type.Name;
        }

        public void Dispose()
        {
            _schema = null;
        }
    }
}
