using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation
{
    /// <summary>
    /// Provides contextual information about the validation of the document.
    /// </summary>
    public class ValidationContext : IProvideUserContext
    {
        private List<ValidationError>? _errors;

        private readonly Dictionary<Operation, List<FragmentDefinition>> _fragments
            = new Dictionary<Operation, List<FragmentDefinition>>();

        private readonly Dictionary<Operation, List<VariableUsage>> _variables =
            new Dictionary<Operation, List<VariableUsage>>();

        internal void Reset()
        {
            _errors = null;
            _fragments.Clear();
            _variables.Clear();
            Operation = null!;
            Schema = null!;
            Document = null!;
            TypeInfo = null!;
            UserContext = null!;
            Variables = null!;
            Extensions = null!;
        }

        /// <summary>
        /// Returns the operation requested to be executed.
        /// </summary>
        public Operation Operation { get; set; } = null!;

        /// <inheritdoc cref="ExecutionContext.Schema"/>
        public ISchema Schema { get; set; } = null!;

        /// <inheritdoc cref="ExecutionContext.Document"/>
        public Document Document { get; set; } = null!;

        /// <inheritdoc cref="Validation.TypeInfo"/>
        public TypeInfo TypeInfo { get; set; } = null!;

        /// <inheritdoc/>
        public IDictionary<string, object?> UserContext { get; set; } = null!;

        /// <summary>
        /// Returns a list of validation errors for this document.
        /// </summary>
        public IEnumerable<ValidationError> Errors => (IEnumerable<ValidationError>?)_errors ?? Array.Empty<ValidationError>();

        /// <summary>
        /// Returns <see langword="true"/> if there are any validation errors for this document.
        /// </summary>
        public bool HasErrors => _errors?.Count > 0;

        /// <inheritdoc cref="ExecutionOptions.Variables"/>
        public Inputs Variables { get; set; } = null!;

        /// <inheritdoc cref="ExecutionOptions.Extensions"/>
        public Inputs Extensions { get; set; } = null!;

        /// <summary>
        /// Adds a validation error to the list of validation errors.
        /// </summary>
        public void ReportError(ValidationError error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error), "Must provide a validation error.");
            (_errors ??= new List<ValidationError>()).Add(error);
        }

        /// <summary>
        /// For a node with a selection set, returns a list of variable references along with what input type each were referenced for.
        /// </summary>
        public List<VariableUsage> GetVariables(IHaveSelectionSet node)
        {
            var usages = new List<VariableUsage>();
            var info = new TypeInfo(Schema);

            var listener = new MatchingNodeVisitor<VariableReference, (List<VariableUsage> usages, TypeInfo info)>((usages, info), (varRef, __, state) => state.usages.Add(new VariableUsage(varRef, state.info.GetInputType()!)));

            new BasicVisitor(info, listener).Visit(node, this);

            return usages;
        }

        /// <summary>
        /// For a specified operation with a document, returns a list of variable references
        /// along with what input type each was referenced for.
        /// </summary>
        public List<VariableUsage> GetRecursiveVariables(Operation operation)
        {
            if (_variables.TryGetValue(operation, out var results))
            {
                return results;
            }

            var usages = GetVariables(operation);

            foreach (var fragment in GetRecursivelyReferencedFragments(operation))
            {
                usages.AddRange(GetVariables(fragment));
            }

            _variables[operation] = usages;

            return usages;
        }

        /// <summary>
        /// Searches the document for a fragment definition by name and returns it.
        /// </summary>
        public FragmentDefinition? GetFragment(string name) => Document.Fragments.FindDefinition(name);

        /// <summary>
        /// Returns a list of fragment spreads within the specified node.
        /// </summary>
        public List<FragmentSpread> GetFragmentSpreads(SelectionSet node)
        {
            var spreads = new List<FragmentSpread>();

            var setsToVisit = new Stack<SelectionSet>();
            setsToVisit.Push(node);

            while (setsToVisit.Count > 0)
            {
                var set = setsToVisit.Pop();

                foreach (var selection in set.SelectionsList)
                {
                    if (selection is FragmentSpread spread)
                    {
                        spreads.Add(spread);
                    }
                    else if (selection is IHaveSelectionSet hasSet && hasSet.SelectionSet != null)
                    {
                        setsToVisit.Push(hasSet.SelectionSet);
                    }
                }
            }

            return spreads;
        }

        /// <summary>
        /// For a specified operation within a document, returns a list of all fragment definitions in use.
        /// </summary>
        public List<FragmentDefinition> GetRecursivelyReferencedFragments(Operation operation)
        {
            if (_fragments.TryGetValue(operation, out var results))
            {
                return results;
            }

            var fragments = new List<FragmentDefinition>();
            var nodesToVisit = new Stack<SelectionSet>();
            nodesToVisit.Push(operation.SelectionSet);
            var collectedNames = new Dictionary<string, bool>();

            while (nodesToVisit.Count > 0)
            {
                var node = nodesToVisit.Pop();

                foreach (var spread in GetFragmentSpreads(node))
                {
                    string fragName = spread.Name;
                    if (!collectedNames.ContainsKey(fragName))
                    {
                        collectedNames[fragName] = true;

                        var fragment = GetFragment(fragName);
                        if (fragment != null)
                        {
                            fragments.Add(fragment);
                            nodesToVisit.Push(fragment.SelectionSet);
                        }
                    }
                }
            }

            _fragments[operation] = fragments;

            return fragments;
        }

        /// <summary>
        /// Returns all of the variable values defined for the operation from the attached <see cref="Variables"/> object.
        /// </summary>
        public Variables GetVariableValues(IVariableVisitor? visitor = null)
        {
            var variableDefinitions = Operation?.Variables;

            if ((variableDefinitions?.List?.Count ?? 0) == 0)
            {
                return Language.AST.Variables.None;
            }

            var variablesObj = new Variables(variableDefinitions!.List!.Count);

            if (variableDefinitions != null)
            {
                foreach (var variableDef in variableDefinitions.List)
                {
                    // find the IGraphType instance for the variable type
                    var graphType = variableDef.Type.GraphTypeFromType(Schema);

                    if (graphType == null)
                    {
                        ReportError(new InvalidVariableError(this, variableDef, variableDef.Name, $"Variable has unknown type '{variableDef.Type.Name()}'"));
                        continue;
                    }

                    // create a new variable object
                    var variable = new Variable(variableDef.Name);

                    // attempt to retrieve the variable value from the inputs
                    if (Variables.TryGetValue(variableDef.Name, out var variableValue))
                    {
                        // parse the variable via ParseValue (for scalars) and ParseDictionary (for objects) as applicable
                        try
                        {
                            variable.Value = GetVariableValue(graphType, variableDef, variableValue, visitor);
                        }
                        catch (ValidationError error)
                        {
                            ReportError(error);
                            continue;
                        }
                    }
                    else if (variableDef.DefaultValue != null)
                    {
                        // if the variable was not specified in the inputs, and a default literal value was specified, use the specified default variable value

                        // parse the variable literal via ParseLiteral (for scalars) and ParseDictionary (for objects) as applicable
                        try
                        {
                            variable.Value = ExecutionHelper.CoerceValue(graphType, variableDef.DefaultValue, variablesObj, null).Value;
                        }
                        catch (Exception ex)
                        {
                            ReportError(new InvalidVariableError(this, variableDef, variableDef.Name, "Error coercing default value.", ex));
                            continue;
                        }
                        variable.IsDefault = true;
                    }
                    else if (graphType is NonNullGraphType)
                    {
                        ReportError(new InvalidVariableError(this, variableDef, variable.Name, "No value provided for a non-null variable."));
                        continue;
                    }

                    // if the variable was not specified and no default was specified, do not set variable.Value

                    // add the variable to the list of parsed variables defined for the operation
                    variablesObj.Add(variable);
                }
            }

            // return the list of parsed variables defined for the operation
            return variablesObj;
        }

        /// <summary>
        /// Return the specified variable's value for the document from the attached <see cref="Variables"/> object.
        /// <br/><br/>
        /// Validates and parses the supplied input object according to the variable's type, and converts the object
        /// with <see cref="ScalarGraphType.ParseValue(object)"/> and
        /// <see cref="IInputObjectGraphType.ParseDictionary(IDictionary{string, object})"/> as applicable.
        /// <br/><br/>
        /// Since v3.3, returns null for variables set to null rather than the variable's default value.
        /// </summary>
        private object? GetVariableValue(IGraphType graphType, VariableDefinition variableDef, object? input, IVariableVisitor? visitor)
        {
            return ParseValue(graphType, variableDef, variableDef.Name, input, visitor);

            // Coerces a value depending on the graph type.
            object? ParseValue(IGraphType type, VariableDefinition variableDef, VariableName variableName, object? value, IVariableVisitor? visitor)
            {
                if (type is IInputObjectGraphType inputObjectGraphType)
                {
                    var parsedValue = ParseValueObject(inputObjectGraphType, variableDef, variableName, value, visitor);
                    visitor?.VisitObject(this, variableDef, variableName, inputObjectGraphType, value, parsedValue);
                    return parsedValue;
                }
                else if (type is NonNullGraphType nonNullGraphType)
                {
                    if (value == null)
                        throw new InvalidVariableError(this, variableDef, variableName, "Received a null input for a non-null variable.");

                    return ParseValue(nonNullGraphType.ResolvedType!, variableDef, variableName, value, visitor);
                }
                else if (type is ListGraphType listGraphType)
                {
                    var parsedValue = ParseValueList(listGraphType, variableDef, variableName, value, visitor);
                    visitor?.VisitList(this, variableDef, variableName, listGraphType, value, parsedValue);
                    return parsedValue;
                }
                else if (type is ScalarGraphType scalarGraphType)
                {
                    var parsedValue = ParseValueScalar(scalarGraphType, variableDef, variableName, value);
                    visitor?.VisitScalar(this, variableDef, variableName, scalarGraphType, value, parsedValue);
                    return parsedValue;
                }
                else
                {
                    throw new InvalidVariableError(this, variableDef, variableName, $"The graph type '{type.Name}' is not an input graph type.");
                }
            }

            // Coerces a scalar value
            object? ParseValueScalar(ScalarGraphType scalarGraphType, VariableDefinition variableDef, VariableName variableName, object? value)
            {
                try
                {
                    return scalarGraphType.ParseValue(value);
                }
                catch (Exception ex)
                {
                    throw new InvalidVariableError(this, variableDef, variableName, $"Unable to convert '{value}' to '{scalarGraphType.Name}'", ex);
                }
            }

            IList<object?>? ParseValueList(ListGraphType listGraphType, VariableDefinition variableDef, VariableName variableName, object? value, IVariableVisitor? visitor)
            {
                if (value == null)
                    return null;

                // note: a list can have a single child element which will automatically be interpreted as a list of 1 elements. (see below rule)
                // so, to prevent a string as being interpreted as a list of chars (which get converted to strings), we ignore considering a string as an IEnumerable
                if (value is IEnumerable values && value is not string)
                {
                    // create a list containing the parsed elements in the input list
                    if (values is IList list)
                    {
                        var valueOutputs = new object?[list.Count];
                        for (int index = 0; index < list.Count; index++)
                        {
                            // parse/validate values as required by graph type
                            valueOutputs[index] = ParseValue(listGraphType.ResolvedType!, variableDef, new VariableName(variableName, index), list[index], visitor);
                        }
                        return valueOutputs;
                    }
                    else
                    {
                        var valueOutputs = new List<object?>(values is ICollection collection ? collection.Count : 0);
                        int index = 0;
                        foreach (object val in values)
                        {
                            // parse/validate values as required by graph type
                            valueOutputs.Add(ParseValue(listGraphType.ResolvedType!, variableDef, new VariableName(variableName, index++), val, visitor));
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
                    object? result = ParseValue(listGraphType.ResolvedType!, variableDef, variableName, value, visitor);
                    return new object?[] { result };
                }
            }

            object? ParseValueObject(IInputObjectGraphType graphType, VariableDefinition variableDef, VariableName variableName, object? value, IVariableVisitor? visitor)
            {
                if (value == null)
                    return null;

                if (value is not IDictionary<string, object?> dic)
                {
                    // RULE: The value for an input object should be an input object literal
                    // or an unordered map supplied by a variable, otherwise a query error
                    // must be thrown.

                    throw new InvalidVariableError(this, variableDef, variableName, $"Unable to parse input as a '{graphType.Name}' type. Did you provide a List or Scalar value accidentally?");
                }

                var newDictionary = new Dictionary<string, object?>(dic.Count);
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
                        object? parsedFieldValue = ParseValue(field.ResolvedType!, variableDef, childFieldVariableName, fieldValue, visitor);
                        visitor?.VisitField(this, variableDef, childFieldVariableName, graphType, field, fieldValue, parsedFieldValue);
                        newDictionary[field.Name] = parsedFieldValue;
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
                        throw new InvalidVariableError(this, variableDef, childFieldVariableName, "No value provided for a non-null variable.");
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
                List<string>? unknownFields = null;
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

                    if (!match)
                        (unknownFields ??= new List<string>(1)).Add(key);
                }

                if (unknownFields?.Count > 0)
                {
                    throw new InvalidVariableError(this, variableDef, variableName, $"Unrecognized input fields {string.Join(", ", unknownFields.Select(k => $"'{k}'"))} for type '{graphType.Name}'.");
                }

                return graphType.ParseDictionary(newDictionary);
            }
        }

        /// <summary>
        /// Validates that the specified AST value is valid for the specified scalar or input graph type.
        /// Graph types that are lists or non-null types are handled appropriately by this method.
        /// Returns a string representing the errors encountered while validating the value.
        /// </summary>
        public string? IsValidLiteralValue(IGraphType type, IValue valueAst)
        {
            if (type is NonNullGraphType nonNull)
            {
                var ofType = nonNull.ResolvedType!;

                if (valueAst == null || valueAst is NullValue)
                {
                    if (ofType != null)
                    {
                        return $"Expected '{ofType.Name}!', found null.";
                    }

                    return "Expected non-null value, found null";
                }

                return IsValidLiteralValue(ofType, valueAst);
            }
            else if (valueAst is NullValue)
            {
                return null;
            }

            if (valueAst == null)
            {
                return null;
            }

            // This function only tests literals, and assumes variables will provide
            // values of the correct type.
            if (valueAst is VariableReference)
            {
                return null;
            }

            if (type is ListGraphType list)
            {
                var ofType = list.ResolvedType!;

                if (valueAst is ListValue listValue)
                {
                    List<string>? errors = null;

                    for (int index = 0; index < listValue.ValuesList.Count; ++index)
                    {
                        string? error = IsValidLiteralValue(ofType, listValue.ValuesList[index]);
                        if (error != null)
                            (errors ??= new List<string>()).Add($"In element #{index + 1}: [{error}]");
                    }

                    return errors == null
                        ? null
                        : string.Join(" ", errors);
                }

                return IsValidLiteralValue(ofType, valueAst);
            }

            if (type is IInputObjectGraphType inputType)
            {
                if (valueAst is not ObjectValue objValue)
                {
                    return $"Expected '{inputType.Name}', found not an object.";
                }

                var fields = inputType.Fields.ToList();
                var fieldAsts = objValue.ObjectFields.ToList();

                List<string>? errors = null;

                // ensure every provided field is defined
                foreach (var providedFieldAst in fieldAsts)
                {
                    var found = fields.Find(x => x.Name == providedFieldAst.Name);
                    if (found == null)
                    {
                        (errors ??= new List<string>()).Add($"In field '{providedFieldAst.Name}': Unknown field.");
                    }
                }

                // ensure every defined field is valid
                foreach (var field in fields)
                {
                    var fieldAst = fieldAsts.Find(x => x.Name == field.Name);

                    if (fieldAst != null)
                    {
                        string? error = IsValidLiteralValue(field.ResolvedType!, fieldAst.Value);
                        if (error != null)
                            (errors ??= new List<string>()).Add($"In field '{field.Name}': [{error}]");
                    }
                    else if (field.ResolvedType is NonNullGraphType nonNull2 && field.DefaultValue == null)
                    {
                        (errors ??= new List<string>()).Add($"Missing required field '{field.Name}' of type '{nonNull2.ResolvedType}'.");
                    }
                }

                return errors == null
                    ? null
                    : string.Join(" ", errors);
            }

            if (type is ScalarGraphType scalar)
            {
                return scalar.CanParseLiteral(valueAst)
                    ? null
                    : $"Expected type '{type.Name}', found {valueAst.StringFrom(Document)}.";
            }

            throw new ArgumentOutOfRangeException(nameof(type), $"Type {type?.Name ?? "<NULL>"} is not a valid input graph type.");
        }
    }
}
