using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    public static class ExecutionHelper
    {
        public static Variables GetVariableValues(Document document, ISchema schema, VariableDefinitions variableDefinitions, Inputs inputs)
        {
            var variables = new Variables();
            variableDefinitions?.Apply(v =>
            {
                var variable = new Variable();
                variable.Name = v.Name;

                object variableValue = null;
                inputs?.TryGetValue(v.Name, out variableValue);
                variable.Value = GetVariableValue(document, schema, v, variableValue);

                variables.Add(variable);
            });
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

            if (input == null)
            {
                if (variable.DefaultValue != null)
                {
                    return variable.DefaultValue.Value;
                }
            }
            var coercedValue = CoerceValue(schema, type, input.AstFromValue(schema, type));
            return coercedValue;
        }

        public static void AssertValidValue(ISchema schema, IGraphType type, object input, string fieldName)
        {
            if (type is NonNullGraphType)
            {
                var nonNullType = ((NonNullGraphType)type).ResolvedType;

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

            if (type is ScalarGraphType)
            {
                var scalar = (ScalarGraphType)type;
                if (ValueFromScalar(scalar, input) == null)
                    throw new InvalidValueException(fieldName, "Invalid Scalar value for input field.");

                return;
            }

            if (type is ListGraphType)
            {
                var listType = (ListGraphType)type;
                var listItemType = listType.ResolvedType;

                var list = input as IEnumerable;
                if (list != null && !(input is string))
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
                var dict = input as Dictionary<string, object>;
                var complexType = (IComplexGraphType)type;

                if (dict == null)
                {
                    throw new InvalidValueException(fieldName,
                        $"Unable to parse input as a '{type.Name}' type. Did you provide a List or Scalar value accidentally?");
                }

                // ensure every provided field is defined
                var unknownFields = type is IInputObjectGraphType
                    ? dict.Keys.Where(key => complexType.Fields.All(field => field.Name != key)).ToArray()
                    : null;

                if (unknownFields != null && unknownFields.Any())
                {
                    throw new InvalidValueException(fieldName,
                        $"Unrecognized input fields {string.Join(", ", unknownFields.Select(k => $"'{k}'"))} for type '{type.Name}'.");
                }

                foreach (var field in complexType.Fields)
                {
                    object fieldValue;
                    dict.TryGetValue(field.Name, out fieldValue);
                    AssertValidValue(schema, field.ResolvedType, fieldValue, $"{fieldName}.{field.Name}");
                }
                return;
            }

            throw new InvalidValueException(fieldName ?? "input", "Invalid input");
        }

        private static object ValueFromScalar(ScalarGraphType scalar, object input)
        {
            if (input is IValue)
            {
                return scalar.ParseLiteral((IValue)input);
            }

            return scalar.ParseValue(input);
        }

        public static Dictionary<string, object> GetArgumentValues(ISchema schema, QueryArguments definitionArguments, Arguments astArguments, Variables variables)
        {
            if (definitionArguments == null || !definitionArguments.Any())
            {
                return null;
            }

            return definitionArguments.Aggregate(new Dictionary<string, object>(), (acc, arg) =>
            {
                var value = astArguments?.ValueFor(arg.Name);
                var type = arg.ResolvedType;

                var coercedValue = CoerceValue(schema, type, value, variables);
                coercedValue = coercedValue ?? arg.DefaultValue;
                acc[arg.Name] = coercedValue;

                return acc;
            });
        }

        public static object CoerceValue(ISchema schema, IGraphType type, IValue input, Variables variables = null)
        {
            if (type is NonNullGraphType)
            {
                var nonNull = type as NonNullGraphType;
                return CoerceValue(schema, nonNull.ResolvedType, input, variables);
            }

            if (input == null)
            {
                return null;
            }

            var variable = input as VariableReference;
            if (variable != null)
            {
                return variables?.ValueFor(variable.Name);
            }

            if (type is ListGraphType)
            {
                var listType = type as ListGraphType;
                var listItemType = listType.ResolvedType;
                var list = input as ListValue;
                return list != null
                    ? list.Values.Map(item => CoerceValue(schema, listItemType, item, variables)).ToArray()
                    : new[] { CoerceValue(schema, listItemType, input, variables) };
            }

            if (type is IObjectGraphType || type is IInputObjectGraphType)
            {
                var complexType = type as IComplexGraphType;
                var obj = new Dictionary<string, object>();

                var objectValue = input as ObjectValue;
                if (objectValue == null)
                {
                    return null;
                }

                complexType.Fields.Apply(field =>
                {
                    var objectField = objectValue.Field(field.Name);
                    if (objectField != null)
                    {
                        var fieldValue = CoerceValue(schema, field.ResolvedType, objectField.Value, variables);
                        fieldValue = fieldValue ?? field.DefaultValue;

                        obj[field.Name] = fieldValue;
                    }
                });

                return obj;
            }

            if (type is ScalarGraphType)
            {
                var scalarType = type as ScalarGraphType;
                return scalarType.ParseLiteral(input);
            }

            return null;
        }

        public static Dictionary<string, Field> CollectFields(
            ExecutionContext context,
            IGraphType specificType,
            SelectionSet selectionSet,
            Dictionary<string, Field> fields,
            List<string> visitedFragmentNames)
        {
            if (fields == null)
            {
                fields = new Dictionary<string, Field>();
            }

            selectionSet?.Selections.Apply(selection =>
            {
                if (selection is Field)
                {
                    var field = (Field)selection;
                    if (!ShouldIncludeNode(context, field.Directives))
                    {
                        return;
                    }

                    var name = field.Alias ?? field.Name;
                    fields[name] = field;
                }
                else if (selection is FragmentSpread)
                {
                    var spread = (FragmentSpread)selection;

                    if (visitedFragmentNames.Contains(spread.Name)
                        || !ShouldIncludeNode(context, spread.Directives))
                    {
                        return;
                    }

                    visitedFragmentNames.Add(spread.Name);

                    var fragment = context.Fragments.FindDefinition(spread.Name);
                    if (fragment == null
                        || !ShouldIncludeNode(context, fragment.Directives)
                        || !DoesFragmentConditionMatch(context, fragment.Type.Name, specificType))
                    {
                        return;
                    }

                    CollectFields(context, specificType, fragment.SelectionSet, fields, visitedFragmentNames);
                }
                else if (selection is InlineFragment)
                {
                    var inline = (InlineFragment)selection;

                    var name = inline.Type != null ? inline.Type.Name : specificType.Name;

                    if (!ShouldIncludeNode(context, inline.Directives)
                      || !DoesFragmentConditionMatch(context, name, specificType))
                    {
                        return;
                    }

                    CollectFields(context, specificType, inline.SelectionSet, fields, visitedFragmentNames);
                }
            });

            return fields;
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

                    object ifObj;
                    values.TryGetValue("if", out ifObj);

                    bool ifVal;
                    return !(bool.TryParse(ifObj?.ToString() ?? string.Empty, out ifVal) && ifVal);
                }

                directive = directives.Find(DirectiveGraphType.Include.Name);
                if (directive != null)
                {
                    var values = GetArgumentValues(
                        context.Schema,
                        DirectiveGraphType.Include.Arguments,
                        directive.Arguments,
                        context.Variables);

                    object ifObj;
                    values.TryGetValue("if", out ifObj);

                    bool ifVal;
                    return bool.TryParse(ifObj?.ToString() ?? string.Empty, out ifVal) && ifVal;
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

            if (conditionalType is IAbstractGraphType)
            {
                var abstractType = (IAbstractGraphType)conditionalType;
                return abstractType.IsPossibleType(type);
            }

            return false;
        }

        /// <summary>
        /// Unwrap nested Tasks to get the result
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static async Task<object> UnwrapResultAsync(object result)
        {
            while (result is Task task)
            {
                await task.ConfigureAwait(false);

                // Most performant if available
                if (task is Task<object> t)
                {
                    result = t.Result;
                }
                else
                {
                    result = ((dynamic)task).Result;
                }
            }

            return result;
        }

        public static IDictionary<string, Field> SubFieldsFor(ExecutionContext context, IGraphType fieldType, Field field)
        {
            var selections = field?.SelectionSet?.Selections;
            //if the field has no subfields
            if (selections == null || selections.Any() == false)
            {
                return null;
            }

            var subFields = new Dictionary<string, Field>();
            var visitedFragments = new List<string>();
            var fields = CollectFields(context, fieldType, field.SelectionSet, subFields, visitedFragments);
            return fields;
        }


    }
}
