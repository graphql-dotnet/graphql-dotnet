using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Introspection;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    public static class ExecutionHelper
    {
        public static IObjectGraphType GetOperationRootType(Document document, ISchema schema, Operation operation)
        {
            IObjectGraphType type;

            ExecutionError error;

            switch (operation.OperationType)
            {
                case OperationType.Query:
                    type = schema.Query;
                    break;

                case OperationType.Mutation:
                    type = schema.Mutation;
                    if (type == null)
                    {
                        error = new ExecutionError("Schema is not configured for mutations");
                        error.AddLocation(operation, document);
                        throw error;
                    }
                    break;

                case OperationType.Subscription:
                    type = schema.Subscription;
                    if (type == null)
                    {
                        error = new ExecutionError("Schema is not configured for subscriptions");
                        error.AddLocation(operation, document);
                        throw error;
                    }
                    break;

                default:
                    error = new ExecutionError("Can only execute queries, mutations and subscriptions.");
                    error.AddLocation(operation, document);
                    throw error;
            }

            return type;
        }

        public static FieldType GetFieldDefinition(Document document, ISchema schema, IObjectGraphType parentType, Field field)
        {
            if (field.Name == SchemaIntrospection.SchemaMeta.Name && schema.Query == parentType)
            {
                return SchemaIntrospection.SchemaMeta;
            }
            if (field.Name == SchemaIntrospection.TypeMeta.Name && schema.Query == parentType)
            {
                return SchemaIntrospection.TypeMeta;
            }
            if (field.Name == SchemaIntrospection.TypeNameMeta.Name)
            {
                return SchemaIntrospection.TypeNameMeta;
            }

            if (parentType == null)
            {
                var error = new ExecutionError($"Schema is not configured correctly to fetch {field.Name}.  Are you missing a root type?");
                error.AddLocation(field, document);
                throw error;
            }

            return parentType.GetField(field.Name);
        }

        public static Variables GetVariableValues(Document document, ISchema schema, VariableDefinitions variableDefinitions, Inputs inputs)
        {
            var variables = new Variables();

            if (variableDefinitions != null)
            {
                foreach (var v in variableDefinitions)
                {
                    var variable = new Variable
                    {
                        Name = v.Name
                    };

                    object variableValue = null;
                    inputs?.TryGetValue(v.Name, out variableValue);
                    variable.Value = GetVariableValue(document, schema, v, variableValue);

                    variables.Add(variable);
                }
            }

            return variables;
        }

        public static object GetVariableValue(Document document, ISchema schema, VariableDefinition variable, object input)
        {
            var type = variable.Type.GraphTypeFromType(schema);

            try
            {
                AssertValidValue(schema, type, input, variable.Name);
            }
            catch (InvalidValueException error)
            {
                error.AddLocation(variable, document);
                throw;
            }

            if (input == null && variable.DefaultValue != null)
            {
                return variable.DefaultValue.Value;
            }

            return CoerceValue(schema, type, input.AstFromValue(schema, type));
        }

        public static void AssertValidValue(ISchema schema, IGraphType type, object input, string fieldName)
        {
            if (type is NonNullGraphType graphType)
            {
                var nonNullType = graphType.ResolvedType;

                if (input == null)
                {
                    throw new InvalidValueException(fieldName, "Received a null input for a non-null field.");
                }

                AssertValidValue(schema, nonNullType, input, fieldName);
                return;
            }

            if (input == null)
            {
                return;
            }

            if (type is ScalarGraphType scalar)
            {
                if (ValueFromScalar(scalar, input) == null)
                    throw new InvalidValueException(fieldName, "Invalid Scalar value for input field.");

                return;
            }

            if (type is ListGraphType listType)
            {
                var listItemType = listType.ResolvedType;

                if (input is IEnumerable list && !(input is string))
                {
                    var index = -1;
                    foreach (var item in list)
                        AssertValidValue(schema, listItemType, item, $"{fieldName}[{++index}]");
                }
                else
                {
                    AssertValidValue(schema, listItemType, input, fieldName);
                }
                return;
            }

            if (type is IObjectGraphType || type is IInputObjectGraphType)
            {
                var complexType = (IComplexGraphType)type;

                if (!(input is Dictionary<string, object> dict))
                {
                    throw new InvalidValueException(fieldName,
                        $"Unable to parse input as a '{type.Name}' type. Did you provide a List or Scalar value accidentally?");
                }

                // ensure every provided field is defined
                IList<string> unknownFields = null;

                if (type is IInputObjectGraphType)
                {
                    unknownFields = dict.Keys
                        .Except(complexType.Fields.Select(f => f.Name))
                        .ToList();
                }

                if (unknownFields?.Count > 0)
                {
                    throw new InvalidValueException(fieldName,
                        $"Unrecognized input fields {string.Join(", ", unknownFields.Select(k => $"'{k}'"))} for type '{type.Name}'.");
                }

                foreach (var field in complexType.Fields)
                {
                    dict.TryGetValue(field.Name, out object fieldValue);
                    AssertValidValue(schema, field.ResolvedType, fieldValue, $"{fieldName}.{field.Name}");
                }
                return;
            }

            throw new InvalidValueException(fieldName ?? "input", "Invalid input");
        }

        private static object ValueFromScalar(ScalarGraphType scalar, object input)
        {
            if (input is IValue value)
            {
                return scalar.ParseLiteral(value);
            }

            return scalar.ParseValue(input);
        }

        public static Dictionary<string, object> GetArgumentValues(ISchema schema, QueryArguments definitionArguments, Arguments astArguments, Variables variables)
        {
            if (definitionArguments == null || definitionArguments.Count == 0)
            {
                return null;
            }

            var values = new Dictionary<string, object>(definitionArguments.Count);

            foreach (var arg in definitionArguments)
            {
                var value = astArguments?.ValueFor(arg.Name);
                var type = arg.ResolvedType;

                var coercedValue = CoerceValue(schema, type, value, variables) ?? arg.DefaultValue;

                if (coercedValue != null)
                {
                    values[arg.Name] = coercedValue;
                }
            }

            return values;
        }

        public static object CoerceValue(ISchema schema, IGraphType type, IValue input, Variables variables = null)
        {
            if (type is NonNullGraphType nonNull)
            {
                return CoerceValue(schema, nonNull.ResolvedType, input, variables);
            }

            if (input == null || input is NullValue)
            {
                return null;
            }

            if (input is VariableReference variable)
            {
                return variables?.ValueFor(variable.Name);
            }

            if (type is ListGraphType listType)
            {
                var listItemType = listType.ResolvedType;

                if (input is ListValue list)
                {
                    return list.Values
                        .Select(item => CoerceValue(schema, listItemType, item, variables))
                        .ToList();
                }
                else
                {
                    return new[] { CoerceValue(schema, listItemType, input, variables) };
                }
            }

            if (type is IObjectGraphType || type is IInputObjectGraphType)
            {
                if (!(input is ObjectValue objectValue))
                {
                    return null;
                }

                var complexType = type as IComplexGraphType;
                var obj = new Dictionary<string, object>();

                foreach (var field in complexType.Fields)
                {
                    var objectField = objectValue.Field(field.Name);
                    if (objectField != null)
                    {
                        obj[field.Name] = CoerceValue(schema, field.ResolvedType, objectField.Value, variables) ?? field.DefaultValue;
                    }
                    else if (field.DefaultValue != null)
                    {
                        obj[field.Name] = field.DefaultValue;
                    }
                }

                return obj;
            }

            if (type is ScalarGraphType scalarType)
            {
                return scalarType.ParseLiteral(input);
            }

            return null;
        }

        private static Fields CollectFields(
            ExecutionContext context,
            IGraphType specificType,
            SelectionSet selectionSet,
            Fields fields,
            List<string> visitedFragmentNames)
        {
            if (selectionSet != null)
            {
                foreach (var selection in selectionSet.Selections)
                {
                    if (selection is Field field)
                    {
                        if (!ShouldIncludeNode(context, field.Directives))
                        {
                            continue;
                        }

                        fields.Add(field);
                    }
                    else if (selection is FragmentSpread spread)
                    {
                        if (visitedFragmentNames.Contains(spread.Name)
                            || !ShouldIncludeNode(context, spread.Directives))
                        {
                            continue;
                        }

                        visitedFragmentNames.Add(spread.Name);

                        var fragment = context.Fragments.FindDefinition(spread.Name);
                        if (fragment == null
                            || !ShouldIncludeNode(context, fragment.Directives)
                            || !DoesFragmentConditionMatch(context, fragment.Type.Name, specificType))
                        {
                            continue;
                        }

                        CollectFields(context, specificType, fragment.SelectionSet, fields, visitedFragmentNames);
                    }
                    else if (selection is InlineFragment inline)
                    {
                        var name = inline.Type != null ? inline.Type.Name : specificType.Name;

                        if (!ShouldIncludeNode(context, inline.Directives)
                          || !DoesFragmentConditionMatch(context, name, specificType))
                        {
                            continue;
                        }

                        CollectFields(context, specificType, inline.SelectionSet, fields, visitedFragmentNames);
                    }

                }
            }

            return fields;
        }

        public static Dictionary<string, Field> CollectFields(
            ExecutionContext context,
            IGraphType specificType,
            SelectionSet selectionSet)
        {
            return CollectFields(context, specificType, selectionSet, Fields.Empty(), new List<string>());
        }

        public static bool ShouldIncludeNode(ExecutionContext context, Directives directives)
        {
            if (directives != null)
            {
                var directive = directives.Find(DirectiveGraphType.Skip.Name);
                if (directive != null)
                {
                    var values = GetArgumentValues(
                        context.Schema,
                        DirectiveGraphType.Skip.Arguments,
                        directive.Arguments,
                        context.Variables);

                    values.TryGetValue("if", out object ifObj);

                    return !(bool.TryParse(ifObj?.ToString() ?? string.Empty, out bool ifVal) && ifVal);
                }

                directive = directives.Find(DirectiveGraphType.Include.Name);
                if (directive != null)
                {
                    var values = GetArgumentValues(
                        context.Schema,
                        DirectiveGraphType.Include.Arguments,
                        directive.Arguments,
                        context.Variables);

                    values.TryGetValue("if", out object ifObj);
                    return bool.TryParse(ifObj?.ToString() ?? string.Empty, out bool ifVal) && ifVal;
                }
            }

            return true;
        }

        public static bool DoesFragmentConditionMatch(ExecutionContext context, string fragmentName, IGraphType type)
        {
            if (string.IsNullOrWhiteSpace(fragmentName))
            {
                return true;
            }

            var conditionalType = context.Schema.FindType(fragmentName);

            if (conditionalType == null)
            {
                return false;
            }

            if (conditionalType.Equals(type))
            {
                return true;
            }

            if (conditionalType is IAbstractGraphType abstractType)
            {
                return abstractType.IsPossibleType(type);
            }

            return false;
        }

        public static IDictionary<string, Field> SubFieldsFor(ExecutionContext context, IGraphType fieldType, Field field)
        {
            var selections = field?.SelectionSet?.Selections;
            if (selections == null || selections.Count == 0)
            {
                return null;
            }
            return CollectFields(context, fieldType, field.SelectionSet);
        }

        public static string[] AppendPath(string[] path, string pathSegment)
        {
            var newPath = new string[path.Length + 1];

            path.CopyTo(newPath, 0);

            newPath[path.Length] = pathSegment;

            return newPath;
        }
    }
}
