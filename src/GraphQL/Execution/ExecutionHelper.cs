using System;
using System.Collections.Generic;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <summary>
    /// Provides helper methods for document execution.
    /// </summary>
    public static class ExecutionHelper
    {
        /// <summary>
        /// Returns the root graph type for the execution -- for a specified schema and operation type.
        /// </summary>
        public static IObjectGraphType GetOperationRootType(Document document, ISchema schema, Operation operation)
        {
            IObjectGraphType type;

            switch (operation.OperationType)
            {
                case OperationType.Query:
                    type = schema.Query;
                    break;

                case OperationType.Mutation:
                    type = schema.Mutation;
                    if (type == null)
                        throw new InvalidOperationError("Schema is not configured for mutations").AddLocation(operation, document);
                    break;

                case OperationType.Subscription:
                    type = schema.Subscription;
                    if (type == null)
                        throw new InvalidOperationError("Schema is not configured for subscriptions").AddLocation(operation, document);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), "Can only execute queries, mutations and subscriptions.");
            }

            return type;
        }

        /// <summary>
        /// Returns a <see cref="FieldType"/> for the specified AST <see cref="Field"/> within a specified parent
        /// output graph type within a given schema. For meta-fields, returns the proper meta-field field type.
        /// </summary>
        public static FieldType GetFieldDefinition(ISchema schema, IObjectGraphType parentType, Field field)
        {
            if (field.Name == schema.SchemaMetaFieldType.Name && schema.Query == parentType)
            {
                return schema.SchemaMetaFieldType;
            }
            if (field.Name == schema.TypeMetaFieldType.Name && schema.Query == parentType)
            {
                return schema.TypeMetaFieldType;
            }
            if (field.Name == schema.TypeNameMetaFieldType.Name)
            {
                return schema.TypeNameMetaFieldType;
            }

            if (parentType == null)
            {
                throw new ArgumentNullException(nameof(parentType), $"Schema is not configured correctly to fetch field '{field.Name}'. Are you missing a root type?");
            }

            return parentType.GetField(field.Name);
        }

        /// <summary>
        /// Returns a dictionary of arguments and their values for a field or directive. Values will be retrieved from literals
        /// or variables as specified by the document.
        /// </summary>
        public static Dictionary<string, ArgumentValue> GetArgumentValues(QueryArguments definitionArguments, Arguments astArguments, Variables variables)
        {
            if (definitionArguments == null || definitionArguments.Count == 0)
            {
                return null;
            }

            var values = new Dictionary<string, ArgumentValue>(definitionArguments.Count);

            foreach (var arg in definitionArguments.List)
            {
                var value = astArguments?.ValueFor(arg.Name);
                var type = arg.ResolvedType;

                values[arg.Name] = CoerceValue(type, value, variables, arg.DefaultValue);
            }

            return values;
        }

        /// <summary>
        /// Coerces a literal value to a compatible .NET type for the variable's graph type.
        /// Typically this is a value for a field argument or default value for a variable.
        /// </summary>
        public static ArgumentValue CoerceValue(IGraphType type, IValue input, Variables variables = null, object fieldDefault = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type is NonNullGraphType nonNull)
            {
                // validation rules have verified that this is not null; if the validation rule was not executed, it
                // is assumed that the caller does not wish this check to be executed
                return CoerceValue(nonNull.ResolvedType, input, variables, fieldDefault);
            }

            if (input == null)
            {
                return new ArgumentValue(fieldDefault, ArgumentSource.FieldDefault);
            }

            if (input is NullValue)
            {
                return ArgumentValue.NullLiteral;
            }

            if (input is VariableReference variable)
            {
                if (variables == null)
                    return new ArgumentValue(fieldDefault, ArgumentSource.FieldDefault);

                var found = variables.ValueFor(variable.Name, out var ret);
                return found ? ret : new ArgumentValue(fieldDefault, ArgumentSource.FieldDefault);
            }

            if (type is ListGraphType listType)
            {
                var listItemType = listType.ResolvedType;

                if (input is ListValue list)
                {
                    var count = list.ValuesList.Count;
                    if (count == 0)
                        return new ArgumentValue(Array.Empty<object>(), ArgumentSource.Literal);

                    var values = new object[count];
                    for (int i = 0; i < count; ++i)
                        values[i] = CoerceValue(listItemType, list.ValuesList[i], variables).Value;
                    return new ArgumentValue(values, ArgumentSource.Literal);
                }
                else
                {
                    return new ArgumentValue(new[] { CoerceValue(listItemType, input, variables).Value }, ArgumentSource.Literal);
                }
            }

            if (type is IInputObjectGraphType inputObjectGraphType)
            {
                if (!(input is ObjectValue objectValue))
                {
                    throw new ArgumentOutOfRangeException(nameof(input), $"Expected object value for '{inputObjectGraphType.Name}', found not an object '{input}'.");
                }

                var obj = new Dictionary<string, object>();

                foreach (var field in inputObjectGraphType.Fields.List)
                {
                    // https://spec.graphql.org/June2018/#sec-Input-Objects
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
                        obj[field.Name] = CoerceValue(field.ResolvedType, objectField.Value, variables).Value;
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

                return new ArgumentValue(inputObjectGraphType.ParseDictionary(obj), ArgumentSource.Literal);
            }

            if (type is ScalarGraphType scalarType)
            {
                return new ArgumentValue(scalarType.ParseLiteral(input) ?? throw new ArgumentException($"Unable to convert '{input}' to '{type.Name}'"), ArgumentSource.Literal);
            }

            throw new ArgumentOutOfRangeException(nameof(input), $"Unknown type of input object '{type.GetType()}'");
        }

        /// <summary>
        /// Examines @skip and @include directives for a node and returns a value indicating if the node should be included or not.
        /// <br/><br/>
        /// Note: Neither @skip nor @include has precedence over the other. In the case that both the @skip and @include
        /// directives are provided on the same field or fragment, it must be queried only if the @skip condition
        /// is <see langword="false"/> and the @include condition is <see langword="true"/>. Stated conversely, the field or
        /// fragment must not be queried if either the @skip condition is <see langword="true"/> or the @include condition is <see langword="false"/>.
        /// </summary>
        public static bool ShouldIncludeNode(ExecutionContext context, Directives directives)
        {
            if (directives != null)
            {
                var directive = directives.Find(DirectiveGraphType.Skip.Name);
                if (directive != null)
                {
                    var arg = DirectiveGraphType.Skip.Arguments.Find("if");

                    if ((bool)CoerceValue(arg.ResolvedType, directive.Arguments?.ValueFor(arg.Name), context.Variables, arg.DefaultValue).Value)
                        return false;
                }

                directive = directives.Find(DirectiveGraphType.Include.Name);
                if (directive != null)
                {
                    var arg = DirectiveGraphType.Include.Arguments.Find("if");

                    return (bool)CoerceValue(arg.ResolvedType, directive.Arguments?.ValueFor(arg.Name), context.Variables, arg.DefaultValue).Value;
                }
            }

            return true;
        }
    }
}
