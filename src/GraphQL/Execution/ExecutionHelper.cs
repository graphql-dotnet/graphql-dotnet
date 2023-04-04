using System.Collections.ObjectModel;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Execution
{
    /// <summary>
    /// Provides helper methods for document execution.
    /// </summary>
    public static class ExecutionHelper
    {
        private static readonly IDictionary<string, ArgumentValue> _emptyDirectiveArguments = new ReadOnlyDictionary<string, ArgumentValue>(new Dictionary<string, ArgumentValue>());

        /// <summary>
        /// Returns a dictionary of directives with their arguments values for a node (field, fragment spread, inline fragment).
        /// Values will be retrieved from literals or variables as specified by the document.
        /// </summary>
        public static IDictionary<string, DirectiveInfo>? GetDirectives(IHasDirectivesNode node, Variables? variables, ISchema schema, GraphQLDocument document)
        {
            if (node.Directives == null || node.Directives.Count == 0)
                return null;

            Dictionary<string, DirectiveInfo>? directives = null;

            foreach (var dir in node.Directives.Items)
            {
                var dirDefinition = schema.Directives.Find(dir.Name);

                // KnownDirectivesInAllowedLocations validation rule should handle unknown directives, so
                // if someone purposely removed the validation rule, it would ignore unknown directives
                // while executing the request
                if (dirDefinition == null)
                    continue;

                (directives ??= new())[dirDefinition.Name] = new DirectiveInfo(
                    dirDefinition,
                    GetArguments(dirDefinition.Arguments, dir.Arguments, variables, document, (ASTNode)node, dir) ?? _emptyDirectiveArguments);
            }

            return directives;
        }

        /// <summary>
        /// Returns a dictionary of arguments and their values for a field or directive.
        /// Values will be retrieved from literals or variables as specified by the document.
        /// </summary>
        public static Dictionary<string, ArgumentValue>? GetArguments(QueryArguments? definitionArguments, GraphQLArguments? astArguments, Variables? variables, GraphQLDocument document, ASTNode fieldOrFragmentSpread, GraphQLDirective? directive)
        {
            if (definitionArguments == null || definitionArguments.Count == 0)
                return null;

            var values = new Dictionary<string, ArgumentValue>(definitionArguments.Count);

            foreach (var arg in definitionArguments.List!)
            {
                GraphQLArgument? argNode = null;
                if (astArguments != null)
                {
                    foreach (var node in astArguments)
                    {
                        if (node.Name.Value.Equals(arg.Name))
                        {
                            argNode = node;
                        }
                    }
                }
                var value = argNode?.Value;
                var type = arg.ResolvedType!;

                values[arg.Name] = CoerceValue(type, value, new CoerceValueContext
                {
                    Argument = argNode,
                    Document = document,
                    Directive = directive,
                    ParentNode = fieldOrFragmentSpread,
                    Variables = variables,
                }, arg.DefaultValue);
            }

            return values;
        }

        internal readonly ref struct CoerceValueContext
        {
            public GraphQLDocument? Document { get; init; }
            /// <summary>
            /// This is typically either a field, a fragment spread, or variable definition.
            /// </summary>
            public ASTNode? ParentNode { get; init; }
            public GraphQLDirective? Directive { get; init; }
            public GraphQLArgument? Argument { get; init; }
            public Variables? Variables { get; init; }
        }

        /// <summary>
        /// Coerces a literal value to a compatible .NET type for the variable's graph type.
        /// Typically this is a value for a field argument or default value for a variable.
        /// Exceptions thrown by scalars are passed through.
        /// </summary>
        public static ArgumentValue CoerceValue(IGraphType type, GraphQLValue? input, Variables? variables = null, object? fieldDefault = null)
            => CoerceValue(type, input, new CoerceValueContext { Variables = variables }, fieldDefault);

        /// <summary>
        /// Coerces a literal value to a compatible .NET type for the variable's graph type.
        /// Typically this is a value for a field argument or default value for a variable.
        /// Exceptions thrown by scalars are wrapped in <see cref="InvalidLiteralError"/>
        /// if <see cref="CoerceValueContext.Document"/> and <see cref="CoerceValueContext.ParentNode"/> are set.
        /// </summary>
        internal static ArgumentValue CoerceValue(IGraphType type, GraphQLValue? input, CoerceValueContext context, object? fieldDefault = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type is NonNullGraphType nonNull)
            {
                // validation rules have verified that this is not null; if the validation rule was not executed, it
                // is assumed that the caller does not wish this check to be executed
                return CoerceValue(nonNull.ResolvedType!, input, context, fieldDefault);
            }

            if (input == null)
            {
                return new ArgumentValue(fieldDefault, ArgumentSource.FieldDefault);
            }

            if (input is GraphQLVariable variable)
            {
                if (context.Variables == null)
                    return new ArgumentValue(fieldDefault, ArgumentSource.FieldDefault);

                var found = context.Variables.ValueFor(variable.Name, out var ret);
                return found ? ret : new ArgumentValue(fieldDefault, ArgumentSource.FieldDefault);
            }

            if (type is ScalarGraphType scalarType)
            {
                try
                {
                    return new ArgumentValue(scalarType.ParseLiteral(input), ArgumentSource.Literal);
                }
                catch (Exception ex) when (context.Document != null && context.ParentNode != null)
                {
                    throw new InvalidLiteralError(context.Document, context.ParentNode, context.Directive, context.Argument, input, ex);
                }
            }

            if (input is GraphQLNullValue)
            {
                return ArgumentValue.NullLiteral;
            }

            if (type is ListGraphType listType)
            {
                var listItemType = listType.ResolvedType!;

                if (input is GraphQLListValue list)
                {
                    var count = list.Values?.Count ?? 0;
                    if (count == 0)
                        return new ArgumentValue(Array.Empty<object>(), ArgumentSource.Literal);

                    var values = new object?[count];
                    for (int i = 0; i < count; ++i)
                        values[i] = CoerceValue(listItemType, list.Values![i], context).Value;
                    return new ArgumentValue(values, ArgumentSource.Literal);
                }
                else
                {
                    return new ArgumentValue(new[] { CoerceValue(listItemType, input, context).Value }, ArgumentSource.Literal);
                }
            }

            if (type is IInputObjectGraphType inputObjectGraphType)
            {
                if (input is not GraphQLObjectValue objectValue)
                {
                    if (context.Document != null && context.ParentNode != null)
                    {
                        throw new InvalidLiteralError(context.Document, context.ParentNode, context.Directive, context.Argument, input,
                            $"Expected object value for '{inputObjectGraphType.Name}', found not an object '{input.Print()}'.");
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(input), $"Expected object value for '{inputObjectGraphType.Name}', found not an object '{input.Print()}'.");
                    }
                }

                var obj = new Dictionary<string, object?>();

                foreach (var field in inputObjectGraphType.Fields.List)
                {
                    // https://spec.graphql.org/October2021/#sec-Input-Objects
                    var objectField = objectValue.Field(field.Name);
                    if (objectField != null)
                    {
                        // Rules covered:

                        // If a literal value is provided for an input object field, an entry in the coerced unordered map is
                        // given the result of coercing that value according to the input coercion rules for the type of that field.

                        // If a variable is provided for an input object field, the runtime value of that variable must be used.
                        // If the runtime value is null and the field type is non‐null, a field error must be thrown.
                        // If no runtime value is provided, the variable definition’s default value should be used.
                        // If the variable definition does not provide a default value, the input object field definition’s
                        // default value should be used.

                        // so: do not pass the field's default value to this method, since the field was specified
                        obj[field.Name] = CoerceValue(field.ResolvedType!, objectField.Value, context).Value;
                    }
                    else if (field.DefaultValue != null)
                    {
                        // If no value is provided for a defined input object field and that field definition provides a default value,
                        // the default value should be used.
                        obj[field.Name] = field.DefaultValue;
                    }
                    // Otherwise, if the field is not required, then no entry is added to the coerced unordered map.

                    // Covered by validation rules:
                    // If no default value is provided and the input object field’s type is non‐null, an error should be
                    // thrown.
                }

                try
                {
                    return new ArgumentValue(inputObjectGraphType.ParseDictionary(obj), ArgumentSource.Literal);
                }
                catch (Exception ex) when (context.Document != null && context.ParentNode != null)
                {
                    throw new InvalidLiteralError(context.Document, context.ParentNode, context.Directive, context.Argument, input, ex);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(input), $"Unknown type of input object '{type.GetType()}'");
        }
    }
}
