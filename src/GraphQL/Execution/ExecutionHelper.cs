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
        /// Returns all of the variable values defined for the operation from the attached <see cref="Inputs"/> object.
        /// </summary>
        public static Variables GetVariableValues(Document document, ISchema schema, VariableDefinitions variableDefinitions, Inputs inputs)
        {
            if ((variableDefinitions?.List?.Count ?? 0) == 0)
            {
                return Variables.None;
            }

            var variables = new Variables(variableDefinitions.List.Count);

            if (variableDefinitions != null)
            {
                foreach (var variableDef in variableDefinitions.List)
                {
                    // find the IGraphType instance for the variable type
                    var graphType = variableDef.Type.GraphTypeFromType(schema);

                    if (graphType == null)
                    {
                        var error = new InvalidVariableError(variableDef.Name, $"Variable has unknown type '{variableDef.Type.Name()}'");
                        error.AddLocation(variableDef, document);
                        throw error;
                    }

                    // create a new variable object
                    var variable = new Variable
                    {
                        Name = variableDef.Name
                    };

                    // attempt to retrieve the variable value from the inputs
                    if (inputs.TryGetValue(variableDef.Name, out var variableValue))
                    {
                        // parse the variable via ParseValue (for scalars) and ParseDictionary (for objects) as applicable
                        variable.Value = GetVariableValue(document, graphType, variableDef, variableValue);
                    }
                    else if (variableDef.DefaultValue != null)
                    {
                        // if the variable was not specified in the inputs, and a default literal value was specified, use the specified default variable value

                        // parse the variable literal via ParseLiteral (for scalars) and ParseDictionary (for objects) as applicable
                        variable.Value = CoerceValue(graphType, variableDef.DefaultValue, variables, null).Value;
                        variable.IsDefault = true;
                    }
                    else if (graphType is NonNullGraphType)
                    {
                        ThrowNullError(variable.Name);
                    }

                    // if the variable was not specified and no default was specified, do not set variable.Value

                    // add the variable to the list of parsed variables defined for the operation
                    variables.Add(variable);
                }
            }

            // return the list of parsed variables defined for the operation
            return variables;
        }

        private static void ThrowNullError(string variableName)
            => throw new InvalidVariableError(variableName, "Received a null input for a non-null variable.");

        private struct VariableName
        {
            public VariableName(VariableName variableName, int index)
            {
                Name = variableName;
                Index = index;
                ChildName = null;
            }
            public VariableName(VariableName variableName, string childName)
            {
                if (variableName.ChildName == null)
                {
                    Name = variableName.Name;
                    Index = variableName.Index;
                }
                else
                {
                    Name = variableName;
                    Index = default;
                }
                ChildName = childName;
            }
            public string Name { get; set; }
            public int? Index { get; set; }
            public string ChildName { get; set; }
            public override string ToString() => (!Index.HasValue ? Name : Name + "[" + Index.Value + "]") + (ChildName != null ? '.' + ChildName : null);
            public static implicit operator VariableName(string name) => new VariableName { Name = name };
            public static implicit operator string(VariableName variableName) => variableName.ToString();
        }

        /// <summary>
        /// Return the specified variable's value for the document from the attached <see cref="Inputs"/> object.
        /// <br/><br/>
        /// Validates and parses the supplied input object according to the variable's type, and converts the object
        /// with <see cref="ScalarGraphType.ParseValue(object)"/> and
        /// <see cref="IInputObjectGraphType.ParseDictionary(IDictionary{string, object})"/> as applicable.
        /// <br/><br/>
        /// Since v3.3, returns <see langword="null"/> for variables set to null rather than the variable's default value.
        /// </summary>
        private static object GetVariableValue(Document document, IGraphType graphType, VariableDefinition variable, object input)
        {
            try
            {
                return ParseValue(graphType, variable.Name, input);
            }
            catch (InvalidVariableError error)
            {
                error.AddLocation(variable, document);
                throw;
            }

            // Coerces a value depending on the graph type.
            static object ParseValue(IGraphType type, VariableName variableName, object value)
            {
                if (type is IInputObjectGraphType inputObjectGraphType)
                {
                    return ParseValueObject(inputObjectGraphType, variableName, value);
                }
                else if (type is NonNullGraphType nonNullGraphType)
                {
                    if (value == null)
                        ThrowNullError(variableName);

                    return ParseValue(nonNullGraphType.ResolvedType, variableName, value);
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

            // Coerces a scalar value
            static object ParseValueScalar(ScalarGraphType scalarGraphType, VariableName variableName, object value)
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

            static object ParseValueList(ListGraphType listGraphType, VariableName variableName, object value)
            {
                if (value == null)
                    return null;

                // note: a list can have a single child element which will automatically be interpreted as a list of 1 elements. (see below rule)
                // so, to prevent a string as being interpreted as a list of chars (which get converted to strings), we ignore considering a string as an IEnumerable
                if (value is IEnumerable values && !(value is string))
                {
                    // create a list containing the parsed elements in the input list
                    if (values is IList list)
                    {
                        var valueOutputs = new object[list.Count];
                        for (int index = 0; index < list.Count; index++)
                        {
                            // parse/validate values as required by graph type
                            valueOutputs[index] = ParseValue(listGraphType.ResolvedType, new VariableName(variableName, index), list[index]);
                        }
                        return valueOutputs;
                    }
                    else
                    {
                        var valueOutputs = new List<object>(values is ICollection collection ? collection.Count : 0);
                        int index = 0;
                        foreach (var val in values)
                        {
                            // parse/validate values as required by graph type
                            valueOutputs.Add(ParseValue(listGraphType.ResolvedType, new VariableName(variableName, index++), val));
                        }
                        return valueOutputs;
                    }
                }
                else
                {
                    // RULE: If the value passed as an input to a list type is not a list and not the null value,
                    // then the result of input coercion is a list of size one, where the single item value is the
                    // result of input coercion for the list’s item type on the provided value (note this may apply
                    // recursively for nested lists).
                    var result = ParseValue(listGraphType.ResolvedType, variableName, value);
                    return new object[] { result };
                }
            }

            static object ParseValueObject(IInputObjectGraphType graphType, VariableName variableName, object value)
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
                foreach (var field in graphType.Fields.List)
                {
                    var childFieldVariableName = new VariableName(variableName, field.Name);

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
                    else if (field.DefaultValue != null)
                    {
                        // RULE: If no value is provided for a defined input object field and that
                        // field definition provides a default value, the default value should be used.
                        newDictionary[field.Name] = field.DefaultValue;
                    }
                    else if (field.ResolvedType is NonNullGraphType)
                    {
                        // RULE: If no default value is provided and the input object field’s type is non‐null,
                        // an error should be thrown.
                        ThrowNullError(childFieldVariableName);
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
                List<string> unknownFields = null;
                foreach (var key in dic.Keys)
                {
                    bool match = false;

                    foreach (var field in graphType.Fields.List)
                    {
                        if (key == field.Name)
                        {
                            match = true;
                            break;
                        }
                    }

                    if (!match) (unknownFields ??= new List<string>(1)).Add(key);
                }

                if (unknownFields?.Count > 0)
                {
                    throw new InvalidVariableError(variableName,
                        $"Unrecognized input fields {string.Join(", ", unknownFields.Select(k => $"'{k}'"))} for type '{graphType.Name}'.");
                }

                return graphType.ParseDictionary(newDictionary);
            }
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
                    return ArgumentValue.NullLiteral;
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
                    var values = GetArgumentValues(
                        DirectiveGraphType.Skip.Arguments,
                        directive.Arguments,
                        context.Variables);

                    if (values.TryGetValue("if", out ArgumentValue ifObj) && bool.TryParse(ifObj.Value?.ToString() ?? string.Empty, out bool ifVal) && ifVal)
                        return false;
                }

                directive = directives.Find(DirectiveGraphType.Include.Name);
                if (directive != null)
                {
                    var values = GetArgumentValues(
                        DirectiveGraphType.Include.Arguments,
                        directive.Arguments,
                        context.Variables);

                    return values.TryGetValue("if", out ArgumentValue ifObj) && bool.TryParse(ifObj.Value?.ToString() ?? string.Empty, out bool ifVal) && ifVal;
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

            var conditionalType = context.Schema.AllTypes[fragmentName];

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
