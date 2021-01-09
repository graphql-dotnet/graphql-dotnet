using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        /// Returns all of the variable values defined for the document from the attached <see cref="Inputs"/> object.
        /// </summary>
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

        /// <summary>
        /// Return the specified variable's value for the document from the attached <see cref="Inputs"/> object.
        /// </summary>
        public static object GetVariableValue(Document document, ISchema schema, VariableDefinition variable, object input)
        {
            var type = variable.Type.GraphTypeFromType(schema);

            if (type == null)
                throw new InvalidVariableError(variable.Name, $"Variable has unknown type '{variable.Type.Name()}'");

            if (input == null && variable.DefaultValue != null)
            {
                return variable.DefaultValue.Value;
            }

            try
            {
                return ParseValue(type, variable.Name, input);
            }
            catch (InvalidVariableError error)
            {
                error.AddLocation(variable, document);
                throw;
            }

            static object ParseValue(IGraphType type, string variableName, object value)
            {
                if (type is IInputObjectGraphType inputObjectGraphType)
                {
                    return ParseValueObject(inputObjectGraphType, variableName, value);
                }
                else if (type is NonNullGraphType nonNullGraphType)
                {
                    return ParseValueNonNull(nonNullGraphType, variableName, value);
                }
                else if (type is ListGraphType listGraphType)
                {
                    return ParseValueList(listGraphType, variableName, value);
                }
                else if (type is ScalarGraphType scalarGraphType)
                {
                    return ParseValueScalar(scalarGraphType, variableName, value);
                }
                else
                {
                    throw new InvalidOperationException("The graph type is not an input graph type.");
                }
            }

            static object ParseValueScalar(ScalarGraphType scalarGraphType, string variableName, object value)
            {
                if (value == null)
                    return null;

                object ret;

                try
                {
                    ret = scalarGraphType.ParseValue(value);
                }
                catch (Exception ex)
                {
                    throw new InvalidVariableError(variableName, $"Unable to convert '{value}' to '{scalarGraphType.Name}'", ex);
                }

                if (ret == null)
                    throw new InvalidVariableError(variableName, $"Unable to convert '{value}' to '{scalarGraphType.Name}'");

                return ret;
            }

            static object ParseValueNonNull(NonNullGraphType nonNullGraphType, string variableName, object value)
            {
                if (value == null)
                    ThrowNullError(variableName);

                return ParseValue(nonNullGraphType.ResolvedType, variableName, value);
            }

            static void ThrowNullError(string variableName)
                => throw new InvalidVariableError(variableName, "Received a null input for a non-null variable.");

            static object ParseValueList(ListGraphType listGraphType, string variableName, object value)
            {
                if (value == null)
                    return null;

                if (value is IEnumerable values && !(value is string))
                {
                    var valueOutputs = new List<object>(values is ICollection collection ? collection.Count : 0);
                    int index = 0;
                    foreach (var val in values)
                    {
                        valueOutputs.Add(ParseValue(listGraphType.ResolvedType, variableName + "[" + index++ + "]", val));
                    }
                    return valueOutputs;
                }
                else
                {
                    // RULE: If the value passed as an input to a list type is not a list and not the null value,
                    // then the result of input coercion is a list of size one, where the single item value is the
                    // result of input coercion for the list’s item type on the provided value (note this may apply
                    // recursively for nested lists).
                    var result = ParseValue(listGraphType.ResolvedType, variableName, value);
                    return new List<object>(1) { result };
                }
            }

            static object ParseValueObject(IInputObjectGraphType graphType, string variableName, object value)
            {
                if (value == null)
                    return null;

                if (!(value is IDictionary<string, object> dic))
                {
                    // RULE: The value for an input object should be an input object literal
                    // or an unordered map supplied by a variable, otherwise a query error
                    // must be thrown.

                    throw new InvalidVariableError(variableName, $"Unable to parse input as a '{graphType.Name}' type. Did you provide a List or Scalar value accidentally?");
                }

                var newDictionary = new Dictionary<string, object>(dic.Count);
                foreach (var field in graphType.Fields)
                {
                    var childFieldVariableName = variableName + (variableName.EndsWith("]") ? null : ".") + field.Name;

                    if (dic.TryGetValue(field.Name, out var fieldValue))
                    {
                        // RULE: If the value null was provided for an input object field, and
                        // the field’s type is not a non‐null type, an entry in the coerced
                        // unordered map is given the value null. In other words, there is a
                        // semantic difference between the explicitly provided value null
                        // versus having not provided a value.

                        // Note: we always call ParseValue even for null values, and if it
                        // is a non-null graph type, the NonNullGraphType.ParseValue method will throw an error
                        newDictionary[field.Name] = ParseValue(field.ResolvedType, childFieldVariableName, fieldValue);
                    }
                    else if (field.ResolvedType is NonNullGraphType nonNullGraphType)
                    {
                        if (field.DefaultValue != null)
                        {
                            // RULE: If no value is provided for a defined input object field and that
                            // field definition provides a default value, the default value should be used.
                            newDictionary[field.Name] = field.DefaultValue;
                        }
                        else
                        {
                            // RULE: If no default value is provided and the input object field’s type is non‐null,
                            // an error should be thrown.
                            ThrowNullError(childFieldVariableName);
                        }
                    }

                    // RULE: Otherwise, if the field is not required, then no
                    // entry is added to the coerced unordered map.

                    // so do not do this:    else { newDictionary[field.Name] = null; }
                }

                // RULE: The value for an input object should be an input object literal
                // or an unordered map supplied by a variable, otherwise a query error
                // must be thrown. In either case, the input object literal or unordered
                // map must not contain any entries with names not defined by a field
                // of this input object type, ***otherwise an error must be thrown.***
                var unknownFields = dic.Keys
                    .Except(graphType.Fields.Select(f => f.Name))
                    .ToList();

                if (unknownFields.Count > 0)
                {
                    throw new InvalidVariableError(variableName,
                        $"Unrecognized input fields {string.Join(", ", unknownFields.Select(k => $"'{k}'"))} for type '{graphType.Name}'.");
                }

                return graphType.ParseDictionary(newDictionary);
            }
        }

        /*
        object ParseLiteral(IGraphType graphType, IValue value)
        {
            if (value == null || value is NullValue)
                return null;

            if (!(value is ObjectValue objectValue))
            {
                // RULE: The value for an input object should be an input object literal
                // or an unordered map supplied by a variable, otherwise a query error
                // must be thrown.

                throw new ArgumentNullException(nameof(value), "Supplied value is not an object node");
            }

            var objectFieldsList = objectValue.ObjectFieldsList;
            var newDictionary = new Dictionary<string, object>(objectFieldsList.Count);
            foreach (var field in Fields)
            {
                var objectField = objectFieldsList.Find(o => o.Name == field.Name);
                if (objectField != null)
                {
                    // RULE: If the value null was provided for an input object field, and
                    // the field’s type is not a non‐null type, an entry in the coerced
                    // unordered map is given the value null. In other words, there is a
                    // semantic difference between the explicitly provided value null
                    // versus having not provided a value.

                    // Note: we always call ParseValue even for null values, and if it
                    // is a non-null graph type, the NonNullGraphType.ParseValue method will throw an error
                    newDictionary[field.Name] = ((IInputType)field.ResolvedType).ParseLiteral(objectField.Value);
                }
                else if (field.ResolvedType is NonNullGraphType nonNullGraphType)
                {
                    // RULE: If no value is provided for a defined input object field and that
                    // field definition provides a default value, the default value should be used.
                    if (field.DefaultValue != null)
                        newDictionary[field.Name] = field.DefaultValue;

                    // RULE: If no default value is provided and the input object field’s type is non‐null,
                    // an error should be thrown.
                    newDictionary[field.Name] = nonNullGraphType.ParseLiteral(null); //throws error (unless overridden)
                }

                // RULE: Otherwise, if the field is not required, then no
                // entry is added to the coerced unordered map.

                // so do not do this:    else { newDictionary[field.Name] = null; }
            }

            // RULE: The value for an input object should be an input object literal
            // or an unordered map supplied by a variable, otherwise a query error
            // must be thrown. In either case, the input object literal or unordered
            // map must not contain any entries with names not defined by a field
            // of this input object type, ***otherwise an error must be thrown.***
            foreach (var objectField in objectFieldsList)
            {
                if (!Fields.Any(x => x.Name == objectField.Name))
                    throw new ArgumentException("An value within the list of object values cannot be matched to a field.", nameof(value));
            }

            return ParseDictionary(newDictionary);
        }
        */

        /*
        /// <summary>
        /// Ensures that the specified variable value is valid for the variable's graph type.
        /// </summary>
        public static void AssertValidVariableValue(ISchema schema, IGraphType type, object input, string variableName, bool hasDefaultValue)
        {
            // see also GraphQLExtensions.IsValidLiteralValue
            if (type is NonNullGraphType graphType)
            {
                var nonNullType = graphType.ResolvedType;

                if (input == null && !hasDefaultValue)
                {
                    throw new InvalidVariableError(variableName, "Received a null input for a non-null variable.");
                }

                AssertValidVariableValue(schema, nonNullType, input, variableName, hasDefaultValue);
                return;
            }

            if (input == null)
            {
                return;
            }

            if (type is ScalarGraphType scalar)
            {
                // verify value can be converted successfully

                if (input is IValue value)
                {
                    bool conversionFailed;

                    try
                    {
                        conversionFailed = scalar.ParseLiteral(value) == null;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidVariableError(variableName, $"Unable to convert '{value.Value}' to '{type.Name}'", ex);
                    }

                    if (conversionFailed)
                        throw new InvalidVariableError(variableName, $"Unable to convert '{value.Value}' to '{type.Name}'");
                }
                else
                {
                    bool conversionFailed;

                    try
                    {
                        conversionFailed = scalar.ParseValue(input) == null;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidVariableError(variableName, $"Unable to convert '{input}' to '{type.Name}'", ex);
                    }

                    if (conversionFailed)
                        throw new InvalidVariableError(variableName, $"Unable to convert '{input}' to '{type.Name}'");
                }

                return;
            }

            if (type is ListGraphType listType)
            {
                var listItemType = listType.ResolvedType;

                if (input is IEnumerable list && !(input is string))
                {
                    var index = -1;
                    foreach (var item in list)
                        AssertValidVariableValue(schema, listItemType, item, $"{variableName}[{++index}]", hasDefaultValue);
                }
                else
                {
                    AssertValidVariableValue(schema, listItemType, input, variableName, hasDefaultValue);
                }
                return;
            }

            if (type is IObjectGraphType || type is IInputObjectGraphType)
            {
                var complexType = (IComplexGraphType)type;

                if (!(input is Dictionary<string, object> dict))
                {
                    throw new InvalidVariableError(variableName,
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
                    throw new InvalidVariableError(variableName,
                        $"Unrecognized input fields {string.Join(", ", unknownFields.Select(k => $"'{k}'"))} for type '{type.Name}'.");
                }

                foreach (var field in complexType.Fields)
                {
                    dict.TryGetValue(field.Name, out object fieldValue);
                    AssertValidVariableValue(schema, field.ResolvedType, fieldValue, $"{variableName}.{field.Name}", hasDefaultValue);
                }
                return;
            }

            throw new InvalidVariableError(variableName ?? "input", "Invalid input");
        }
        */

        /// <summary>
        /// Returns a dictionary of arguments and their values for a field or directive. Values will be retrieved from literals
        /// or variables as specified by the document.
        /// </summary>
        public static Dictionary<string, object> GetArgumentValues(ISchema schema, QueryArguments definitionArguments, Arguments astArguments, Variables variables)
        {
            if (definitionArguments == null || definitionArguments.Count == 0)
            {
                return null;
            }

            var values = new Dictionary<string, object>(definitionArguments.Count);

            foreach (var arg in definitionArguments.ArgumentsList)
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

        /// <summary>
        /// Coerces a variable value to a compatible .NET type for the variable's graph type.
        /// </summary>
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

            if (type is IInputObjectGraphType inputObjectGraphType)
            {
                if (!(input is ObjectValue objectValue))
                {
                    return null;
                }

                var obj = new Dictionary<string, object>();

                foreach (var field in inputObjectGraphType.Fields)
                {
                    var objectField = objectValue.Field(field.Name);
                    if (objectField != null)
                    {
                        obj[field.Name] = CoerceValue(schema, field.ResolvedType, objectField.Value, variables)/* ?? field.DefaultValue*/;
                    }
                    else if (field.DefaultValue != null)
                    {
                        obj[field.Name] = field.DefaultValue;
                    }
                }

                return inputObjectGraphType.ParseDictionary(obj);
            }

            if (type is ScalarGraphType scalarType)
            {
                return scalarType.ParseLiteral(input) ?? throw new ArgumentException($"Unable to convert '{input}' to '{type.Name}'");
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
                foreach (var selection in selectionSet.SelectionsList)
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

        /// <summary>
        /// Before execution, the selection set is converted to a grouped field set by calling CollectFields().
        /// Each entry in the grouped field set is a list of fields that share a response key (the alias if defined,
        /// otherwise the field name). This ensures all fields with the same response key included via referenced
        /// fragments are executed at the same time.
        /// <br/><br/>
        /// See http://spec.graphql.org/June2018/#sec-Field-Collection and http://spec.graphql.org/June2018/#CollectFields()
        /// </summary>
        public static Dictionary<string, Field> CollectFields(
            ExecutionContext context,
            IGraphType specificType,
            SelectionSet selectionSet)
        {
            return CollectFields(context, specificType, selectionSet, Fields.Empty(), new List<string>());
        }

        /// <summary>
        /// Examines @skip and @include directives for a node and returns a value indicating if the node should be included or not.
        /// <br/><br/>
        /// Note: Neither @skip nor @include has precedence over the other. In the case that both the @skip and @include
        /// directives are provided on the same field or fragment, it must be queried only if the @skip condition
        /// is false and the @include condition is true. Stated conversely, the field or fragment must not be queried
        /// if either the @skip condition is true or the @include condition is false.
        /// </summary>
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

                    if (values.TryGetValue("if", out object ifObj) && bool.TryParse(ifObj?.ToString() ?? string.Empty, out bool ifVal) && ifVal)
                        return false;
                }

                directive = directives.Find(DirectiveGraphType.Include.Name);
                if (directive != null)
                {
                    var values = GetArgumentValues(
                        context.Schema,
                        DirectiveGraphType.Include.Arguments,
                        directive.Arguments,
                        context.Variables);

                    return values.TryGetValue("if", out object ifObj) && bool.TryParse(ifObj?.ToString() ?? string.Empty, out bool ifVal) && ifVal;
                }
            }

            return true;
        }

        /// <summary>
        /// This method calculates the criterion for matching fragment definition (spread or inline) to a given graph type.
        /// This criterion determines the need to fill the resulting selection set with fields from such a fragment.
        /// <br/><br/>
        /// See http://spec.graphql.org/June2018/#DoesFragmentTypeApply()
        /// </summary>
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
    }
}
