using GraphQL.Introspection;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GraphQL.Utilities
{
    public class SchemaPrinter : IDisposable
    {
        protected SchemaPrinterOptions Options { get; }

        private readonly List<string> _scalars = new List<string>(
            new[]
            {
                "String",
                "Boolean",
                "Int",
                "Float",
                "ID"
            });

        public SchemaPrinter(
            ISchema schema,
            SchemaPrinterOptions options = null)
        {
            Schema = schema;
            Options = options ?? new SchemaPrinterOptions();

            if (Options.CustomScalars?.Count > 0)
            {
                _scalars.AddRange(Options.CustomScalars);
            }
        }

        private ISchema Schema { get; set; }

        public string Print()
        {
            return PrintFilteredSchema(n => !IsSpecDirective(n), IsDefinedType);
        }

        public string PrintIntrospectionSchema()
        {
            return PrintFilteredSchema(IsSpecDirective, IsIntrospectionType);
        }

        public string PrintFilteredSchema(Func<string, bool> directiveFilter, Func<string, bool> typeFilter)
        {
            if (!Schema.Initialized)
            {
                Schema.Initialize();
            }

            var directives = Schema.Directives.Where(d => directiveFilter(d.Name)).OrderBy(d => d.Name, StringComparer.Ordinal).ToList();
            var types = Schema.AllTypes
                .Where(t => typeFilter(t.Name))
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToList();

            var result = new[]
            {
                PrintSchemaDefinition(Schema),
            }
            .Concat(directives.Select(PrintDirective))
            .Concat(types.Select(PrintType))
            .Where(x => x != null)
            .ToList();

            return string.Join(Environment.NewLine + Environment.NewLine, result) + Environment.NewLine;
        }

        public virtual bool IsDefinedType(string typeName)
        {
            return !IsIntrospectionType(typeName) && !IsBuiltInScalar(typeName);
        }

        public bool IsIntrospectionType(string typeName)
        {
            return typeName.StartsWith("__", StringComparison.InvariantCulture);
        }

        public bool IsBuiltInScalar(string typeName)
        {
            return _scalars.Contains(typeName);
        }

        public bool IsSpecDirective(string directiveName)
        {
            var names = new []
            {
                "skip",
                "include",
                "deprecated"
            };
            return names.Contains(directiveName);
        }

        public string PrintSchemaDefinition(ISchema schema)
        {
            if (IsSchemaOfCommonNames(Schema)) return null;

            var operationTypes = new List<string>();

            if (schema.Query != null)
            {
                operationTypes.Add($"  query: {ResolveName(schema.Query)}");
            }

            if (schema.Mutation != null)
            {
                operationTypes.Add($"  mutation: {ResolveName(schema.Mutation)}");
            }

            if (schema.Subscription != null)
            {
                operationTypes.Add($"  subscription: {ResolveName(schema.Subscription)}");
            }

            return $"schema {{{Environment.NewLine}{string.Join(Environment.NewLine, operationTypes)}{Environment.NewLine}}}";
        }

        /**
         * GraphQL schema define root types for each type of operation. These types are
         * the same as any other type and can be named in any manner, however there is
         * a common naming convention:
         *
         *   schema {
         *     query: Query
         *     mutation: Mutation
         *     subscription: Subscription
         *   }
         *
         * When using this naming convention, the schema description can be omitted.
         */
        public bool IsSchemaOfCommonNames(ISchema schema)
        {
            if (schema.Query != null && schema.Query.Name != "Query")
            {
                return false;
            }

            if (schema.Mutation != null && schema.Mutation.Name != "Mutation")
            {
                return false;
            }

            if (schema.Subscription != null && schema.Subscription.Name != "Subscription")
            {
                return false;
            }

            return true;
        }

        public string PrintType(IGraphType type)
        {
            if (type is EnumerationGraphType graphType)
            {
                return PrintEnum(graphType);
            }

            if (type is ScalarGraphType scalarGraphType)
            {
                return PrintScalar(scalarGraphType);
            }

            if (type is IObjectGraphType objectGraphType)
            {
                return PrintObject(objectGraphType);
            }

            if (type is IInterfaceGraphType interfaceGraphType)
            {
                return PrintInterface(interfaceGraphType);
            }

            if (type is UnionGraphType unionGraphType)
            {
                return PrintUnion(unionGraphType);
            }

            if (type is DirectiveGraphType directiveGraphType)
            {
                return PrintDirective(directiveGraphType);
            }

            if (!(type is IInputObjectGraphType))
            {
                throw new InvalidOperationException("Unknown GraphType {0}".ToFormat(type.GetType().Name));
            }

            return PrintInputObject((IInputObjectGraphType)type);
        }

        public string PrintScalar(ScalarGraphType type)
        {
            var description = Options.IncludeDescriptions ? PrintDescription(type.Description) : "";
            return description + "scalar {0}".ToFormat(type.Name);
        }

        public virtual string PrintObject(IObjectGraphType type)
        {
            var description = Options.IncludeDescriptions ? PrintDescription(type.Description) : "";

            var interfaces = type.ResolvedInterfaces.Select(x => x.Name).ToList();
            var delimiter = Options.OldImplementsSyntax ? ", " : " & ";
            var implementedInterfaces = interfaces.Count > 0
                ? " implements {0}".ToFormat(string.Join(delimiter, interfaces))
                : "";

            return description + "type {1}{2} {{{0}{3}{0}}}".ToFormat(Environment.NewLine, type.Name, implementedInterfaces, PrintFields(type));
        }

        public virtual string PrintInterface(IInterfaceGraphType type)
        {
            var description = Options.IncludeDescriptions ? PrintDescription(type.Description) : "";
            return description + "interface {1} {{{0}{2}{0}}}".ToFormat(Environment.NewLine, type.Name, PrintFields(type));
        }

        public string PrintUnion(UnionGraphType type)
        {
            var description = Options.IncludeDescriptions ? PrintDescription(type.Description) : "";
            var possibleTypes = string.Join(" | ", type.PossibleTypes.Select(x => x.Name));
            return description + "union {0} = {1}".ToFormat(type.Name, possibleTypes);
        }

        public string PrintEnum(EnumerationGraphType type)
        {
            var description = Options.IncludeDescriptions ? PrintDescription(type.Description) : "";
            var values = string.Join(Environment.NewLine, type.Values.Select(x => "  " + x.Name));
            return description + "enum {1} {{{0}{2}{0}}}".ToFormat(Environment.NewLine, type.Name, values);
        }

        public string PrintInputObject(IInputObjectGraphType type)
        {
            var description = Options.IncludeDescriptions ? PrintDescription(type.Description) : "";
            var fields = type.Fields.Select(x => "  " + PrintInputValue(x));
            return description + "input {1} {{{0}{2}{0}}}".ToFormat(Environment.NewLine, type.Name, string.Join(Environment.NewLine, fields));
        }

        public virtual string PrintFields(IComplexGraphType type)
        {
            var fields = type?.Fields
                .Select(x =>
                new
                {
                    x.Name,
                    Type = ResolveName(x.ResolvedType),
                    Args = PrintArgs(x),
                    Description = Options.IncludeDescriptions ? PrintDescription(x.Description, "  ") : string.Empty,
                    Deprecation = Options.IncludeDeprecationReasons ? PrintDeprecation(x.DeprecationReason) : string.Empty,
                }).ToList();

            return string.Join(Environment.NewLine, fields?.Select(
                f => "{3}  {0}{1}: {2}{4}".ToFormat(f.Name, f.Args, f.Type, f.Description, f.Deprecation)));
        }

        public string PrintArgs(FieldType field)
        {
            if (field.Arguments == null || field.Arguments.Count == 0)
            {
                return string.Empty;
            }

            return "({0})".ToFormat(string.Join(", ", field.Arguments.Select(PrintInputValue)));
        }

        public string PrintInputValue(FieldType argument)
        {
            var argumentType = argument.ResolvedType;
            var desc = "{0}: {1}".ToFormat(argument.Name, ResolveName(argumentType));

            if (argument.DefaultValue != null)
            {
                desc += " = {0}".ToFormat(FormatDefaultValue(argument.DefaultValue, argumentType));
            }

            return desc;
        }

        public string PrintInputValue(QueryArgument argument)
        {
            var argumentType = argument.ResolvedType;
            var desc = "{0}: {1}".ToFormat(argument.Name, ResolveName(argumentType));

            if (argument.DefaultValue != null)
            {
                desc += " = {0}".ToFormat(FormatDefaultValue(argument.DefaultValue, argumentType));
            }

            return desc;
        }

        public string PrintDirective(DirectiveGraphType directive)
        {
            var builder = new StringBuilder();
            if (Options.IncludeDescriptions)
            {
                builder.Append(PrintDescription(directive.Description));
            }
            builder.AppendLine($"directive @{directive.Name}(");
            builder.AppendLine(FormatDirectiveArguments(directive.Arguments));
            builder.Append($") on {FormatDirectiveLocationList(directive.Locations)}");
            return builder.ToString().TrimStart();
        }

        private string FormatDirectiveArguments(QueryArguments arguments)
        {
            if (arguments == null || arguments.Count == 0) return null;
            return string.Join(Environment.NewLine, arguments.Select(arg=> $"  {PrintInputValue(arg)}"));
        }

        private string FormatDirectiveLocationList(IEnumerable<DirectiveLocation> locations)
        {
            var enums = new __DirectiveLocation();
            return string.Join(" | ", locations.Select(x => enums.Serialize(x)));
        }

        public string FormatDefaultValue(object value, IGraphType graphType)
        {
            if (value is string)
            {
                return "\"{0}\"".ToFormat(value);
            }

            if (value is bool)
            {
                return value.ToString().ToLower(CultureInfo.InvariantCulture);
            }

            if (IsEnumType(graphType))
            {
                return "{0}".ToFormat(SerializeEnumValue(graphType, value));
            }

            return "{0}".ToFormat(value);
        }

        private static bool IsEnumType(IGraphType type)
        {
            return type.GetNamedType() is EnumerationGraphType;
        }

        private static object SerializeEnumValue(IGraphType type, object value)
        {
            if (type is NonNullGraphType nullable)
            {
                type = nullable.ResolvedType;
            }

            return ((EnumerationGraphType)type).Serialize(value);
        }

        public static string ResolveName(IGraphType type)
        {
            if (type is NonNullGraphType nullable)
            {
                return "{0}!".ToFormat(ResolveName(nullable.ResolvedType));
            }

            if (type is ListGraphType list)
            {
                return "[{0}]".ToFormat(ResolveName(list.ResolvedType));
            }

            return type?.Name;
        }

        public string PrintDescription(string description, string indentation = "", bool firstInBlock = true)
        {
            if (string.IsNullOrWhiteSpace(description)) return "";

            indentation ??= "";

            // normalize newlines
            description = description.Replace("\r", "");

            var lines = description.Split('\n');

            var desc = !string.IsNullOrWhiteSpace(indentation) && !firstInBlock ? Environment.NewLine : "";

            lines.Apply(line =>
            {
                if (line == "")
                {
                    desc += indentation + $"#{Environment.NewLine}";
                }
                else
                {
                    // For > 120 character long lines, cut at space boundaries into sublines
                    // of ~80 chars.
                    var sublines = BreakLine(line, 120 - indentation.Length);
                    sublines.Apply(sub => desc += $"{indentation}# {sub}{Environment.NewLine}");
                }
            });

            return desc;
        }

        public string PrintDeprecation(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return string.Empty;
            }
            return $" @deprecated(reason: \"{reason.Replace("\"", "\\\"")}\")";
        }

        public string[] BreakLine(string line, int len)
        {
            if (line.Length < len + 5)
            {
                return new[] { line };
            }
            var parts = Regex.Split(line, $"((?: |^).{{15,{len - 40}}}(?= |$))");
            if (parts.Length < 4)
            {
                return new[] { line };
            }
            var sublines = new List<string>
            {
                parts[0] + parts[1] + parts[2]
            };
            for (var i = 3; i < parts.Length; i += 2)
            {
                sublines.Add(parts[i].Substring(1) + parts[i + 1]);
            }
            return sublines.ToArray();
        }

        public void Dispose()
        {
            Schema = null;
        }
    }
}
