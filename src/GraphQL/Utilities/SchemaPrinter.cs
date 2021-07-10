using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using GraphQL.Introspection;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Enables printing schema as SDL (Schema Definition Language) document.
    /// <br/>
    /// See <see href="http://spec.graphql.org/June2018/#sec-Type-System"/> for more information.
    /// </summary>
    public class SchemaPrinter //TODO: rewrite string concatenations to use buffer ?
    {
        private static readonly List<string> _builtInScalars = new List<string>
        {
            "String",
            "Boolean",
            "Int",
            "Float",
            "ID"
        };

        private static readonly List<string> _builtInDirectives = new List<string>
        {
            "skip",
            "include",
            "deprecated"
        };

        /// <summary>
        /// Creates printer with the specified options.
        /// </summary>
        /// <param name="schema">Schema to print.</param>
        /// <param name="options">Printer options.</param>
        public SchemaPrinter(ISchema schema, SchemaPrinterOptions options = null)
        {
            Schema = schema;
            Options = options ?? new SchemaPrinterOptions();
        }

        protected static bool IsIntrospectionType(string typeName) => typeName.StartsWith("__", StringComparison.InvariantCulture);

        protected static bool IsBuiltInScalar(string typeName) => _builtInScalars.Contains(typeName);

        protected static bool IsBuiltInDirective(string directiveName) => _builtInDirectives.Contains(directiveName);

        private ISchema Schema { get; set; }

        protected SchemaPrinterOptions Options { get; }

        /// <summary>
        /// Prints only 'defined' types and directives.
        /// <br/>
        /// See <see cref="IsDefinedType(string)"/> and <see cref="IsDefinedDirective(string)"/> for more information about what 'defined' means.
        /// </summary>
        /// <returns>SDL document.</returns>
        public string Print() => PrintFilteredSchema(IsDefinedDirective, IsDefinedType);

        /// <summary>
        /// Prints only introspection types.
        /// </summary>
        /// <returns>SDL document.</returns>
        public string PrintIntrospectionSchema() => PrintFilteredSchema(IsBuiltInDirective, IsIntrospectionType);

        /// <summary>
        /// Prints schema according to the specified filters.
        /// </summary>
        /// <param name="directiveFilter">Filter for directives.</param>
        /// <param name="typeFilter">Filter for types.</param>
        /// <returns>SDL document.</returns>
        public string PrintFilteredSchema(Func<string, bool> directiveFilter, Func<string, bool> typeFilter)
        {
            Schema.Initialize();

            var directives = Schema.Directives.Where(d => directiveFilter(d.Name)).OrderBy(d => d.Name, StringComparer.Ordinal).ToList();
            var types = Schema.AllTypes
                .Dictionary
                .Values
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

        /// <summary>
        /// Determines that the specified directive is defined in the schema and should be printed.
        /// By default, all directives are defined (printed) except for built-in directives.
        /// </summary>
        protected virtual bool IsDefinedDirective(string directiveName) => !IsBuiltInDirective(directiveName);

        /// <summary>
        /// Determines that the specified type is defined in the schema and should be printed.
        /// By default, all types are defined (printed) except for introspection types and built-in scalars.
        /// </summary>
        protected virtual bool IsDefinedType(string typeName) => !IsIntrospectionType(typeName) && !IsBuiltInScalar(typeName);

        public string PrintSchemaDefinition(ISchema schema)
        {
            if (IsSchemaOfCommonNames(Schema))
                return null;

            var operationTypes = new List<string>();

            if (schema.Query != null)
            {
                operationTypes.Add($"  query: {schema.Query}");
            }

            if (schema.Mutation != null)
            {
                operationTypes.Add($"  mutation: {schema.Mutation}");
            }

            if (schema.Subscription != null)
            {
                operationTypes.Add($"  subscription: {schema.Subscription}");
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
            return type switch
            {
                EnumerationGraphType graphType => PrintEnum(graphType),
                ScalarGraphType scalarGraphType => PrintScalar(scalarGraphType),
                IObjectGraphType objectGraphType => PrintObject(objectGraphType),
                IInterfaceGraphType interfaceGraphType => PrintInterface(interfaceGraphType),
                UnionGraphType unionGraphType => PrintUnion(unionGraphType),
                DirectiveGraphType directiveGraphType => PrintDirective(directiveGraphType),  //TODO: DirectiveGraphType does not inherit IGraphType
                IInputObjectGraphType input => PrintInputObject(input),
                _ => throw new InvalidOperationException($"Unknown GraphType '{type.GetType().Name}' with name '{type.Name}'")
            };
        }

        public string PrintScalar(ScalarGraphType type) => $"{FormatDescription(type.Description)}scalar {type.Name}";

        public virtual string PrintObject(IObjectGraphType type)
        {
            var interfaces = type.ResolvedInterfaces.List.Select(x => x.Name).ToList();
            var delimiter = Options.OldImplementsSyntax ? ", " : " & ";
            var implementedInterfaces = interfaces.Count > 0
                ? " implements {0}".ToFormat(string.Join(delimiter, interfaces))
                : "";

            return FormatDescription(type.Description) + "type {1}{2} {{{0}{3}{0}}}".ToFormat(Environment.NewLine, type.Name, implementedInterfaces, PrintFields(type));
        }

        public virtual string PrintInterface(IInterfaceGraphType type)
        {
            return FormatDescription(type.Description) + "interface {1} {{{0}{2}{0}}}".ToFormat(Environment.NewLine, type.Name, PrintFields(type));
        }

        public string PrintUnion(UnionGraphType type)
        {
            var possibleTypes = string.Join(" | ", type.PossibleTypes.Select(x => x.Name));
            return FormatDescription(type.Description) + "union {0} = {1}".ToFormat(type.Name, possibleTypes);
        }

        public string PrintEnum(EnumerationGraphType type)
        {
            var values = string.Join(Environment.NewLine, type.Values.Select(x => FormatDescription(x.Description, "  ") + "  " + x.Name + (Options.IncludeDeprecationReasons ? PrintDeprecation(x.DeprecationReason) : "")));
            return FormatDescription(type.Description) + "enum {1} {{{0}{2}{0}}}".ToFormat(Environment.NewLine, type.Name, values);
        }

        public string PrintInputObject(IInputObjectGraphType type)
        {
            var fields = type.Fields.Select(x => PrintInputValue(x));
            return FormatDescription(type.Description) + "input {1} {{{0}{2}{0}}}".ToFormat(Environment.NewLine, type.Name, string.Join(Environment.NewLine, fields));
        }

        public virtual string PrintFields(IComplexGraphType type)
        {
            var fields = type?.Fields
                .Select(x =>
                new
                {
                    x.Name,
                    Type = x.ResolvedType,
                    Args = PrintArgs(x),
                    Description = FormatDescription(x.Description, "  "),
                    Deprecation = Options.IncludeDeprecationReasons ? PrintDeprecation(x.DeprecationReason) : "",
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

            return "({0})".ToFormat(string.Join(", ", field.Arguments.Select(PrintInputValue))); //TODO: iterator allocation
        }

        public string PrintInputValue(FieldType field)
        {
            var argumentType = field.ResolvedType;
            var description = $"{FormatDescription(field.Description, "  ")}  {field.Name}: {argumentType}";

            if (field.DefaultValue != null)
            {
                description += " = {0}".ToFormat(FormatDefaultValue(field.DefaultValue, argumentType));
            }

            return description;
        }

        public string PrintInputValue(QueryArgument argument)
        {
            var argumentType = argument.ResolvedType;
            var desc = "{0}: {1}".ToFormat(argument.Name, argumentType);

            if (argument.DefaultValue != null)
            {
                desc += " = {0}".ToFormat(FormatDefaultValue(argument.DefaultValue, argumentType));
            }

            return desc;
        }

        public string PrintDirective(DirectiveGraphType directive)
        {
            var builder = new StringBuilder();
            builder.Append(FormatDescription(directive.Description));
            builder.Append($"directive @{directive.Name}");

            if (directive.Arguments?.Count > 0)
            {
                builder.AppendLine("(");
                builder.AppendLine(FormatDirectiveArguments(directive.Arguments));
                builder.Append(')');
            }

            if (directive.Repeatable)
                builder.Append(" repeatable");

            builder.Append($" on {FormatDirectiveLocationList(directive.Locations)}");
            return builder.ToString().TrimStart();
        }

        private string FormatDirectiveArguments(QueryArguments arguments)
        {
            if (arguments == null || arguments.Count == 0)
                return null;
            return string.Join(Environment.NewLine, arguments.Select(arg => $"  {PrintInputValue(arg)}"));
        }

        private string FormatDirectiveLocationList(IEnumerable<DirectiveLocation> locations)
        {
            return string.Join(" | ", locations.Select(x => __DirectiveLocation.Instance.Serialize(x))); //TODO: remove allocations
        }

        protected string FormatDescription(string description, string indentation = "")
        {
            if (Options.IncludeDescriptions)
            {
                if (Options.PrintDescriptionsAsComments)
                {
                    return PrintComment(description, indentation);
                }
                else
                {
                    return PrintDescription(description, indentation);
                }
            }
            return "";
        }

        public string FormatDefaultValue(object value, IGraphType graphType)
        {
            return graphType switch
            {
                NonNullGraphType nonNull => FormatDefaultValue(value, nonNull.ResolvedType),
                ListGraphType list => "[{0}]".ToFormat(string.Join(", ", ((IEnumerable<object>)value).Select(i => FormatDefaultValue(i, list.ResolvedType)))),
                IInputObjectGraphType input => FormatInputObjectValue(value, input),
                EnumerationGraphType enumeration => (enumeration.Serialize(value) ?? throw new ArgumentOutOfRangeException(nameof(value), $"Unknown value '{value}' for enumeration '{enumeration.Name}'")).ToString(),
                ScalarGraphType scalar => AstPrinter.Print(scalar.ToAST(value)),
                _ => throw new NotSupportedException($"Unsupported graph type '{graphType}'")
            };
        }

        private string FormatInputObjectValue(object value, IInputObjectGraphType input)
        {
            var sb = new StringBuilder();
            sb.Append("{ ");

            foreach (var field in input.Fields)
            {
                string propertyName = field.GetMetadata<string>(ComplexGraphType<object>.ORIGINAL_EXPRESSION_PROPERTY_NAME) ?? field.Name;
                PropertyInfo propertyInfo;

                try
                {
                    propertyInfo = value.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                }
                catch (AmbiguousMatchException)
                {
                    propertyInfo = value.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                }

                object propertyValue = propertyInfo.GetValue(value);
                if (propertyValue != null)
                {
                    sb.Append(field.Name)
                       .Append(": ")
                       .Append(FormatDefaultValue(propertyValue, field.ResolvedType))
                       .Append(", ");
                }
            }

            sb.Length -= 2;
            sb.Append(" }");
            return sb.ToString();
        }

        public virtual string PrintComment(string comment, string indentation = "", bool firstInBlock = true)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return "";

            indentation ??= "";

            // normalize newlines
            comment = comment.Replace("\r", "");

            var lines = comment.Split('\n');

            var desc = !string.IsNullOrWhiteSpace(indentation) && !firstInBlock ? Environment.NewLine : "";

            foreach (var line in lines)
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
                    foreach (string sub in sublines)
                        desc += $"{indentation}# {sub}{Environment.NewLine}";
                }
            }

            return desc;
        }

        public string PrintDescription(string description, string indentation = "", bool firstInBlock = true)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "";

            indentation ??= "";

            // escape """ with \"""
            description = description.Replace("\"\"\"", "\\\"\"\"");

            // normalize newlines
            description = description.Replace("\r", "");

            // remove control characters besides newline and tab
            if (description.Any(c => c < ' ' && c != '\t' & c != '\n'))
            {
                description = new string(description.Where(c => c >= ' ' || c == '\t' || c == '\n').ToArray());
            }

            var lines = description.Split('\n');

            var desc = !string.IsNullOrWhiteSpace(indentation) && !firstInBlock ? Environment.NewLine : "";

            desc += indentation + "\"\"\"\n";

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    desc += Environment.NewLine;
                }
                else
                {
                    desc += indentation + line + Environment.NewLine;
                }
            }

            desc += indentation + "\"\"\"\n";

            return desc;
        }

        public string PrintDeprecation(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return string.Empty;
            }
            return $" @deprecated(reason: {AstPrinter.Print(new StringValue(reason))})";
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
    }
}
