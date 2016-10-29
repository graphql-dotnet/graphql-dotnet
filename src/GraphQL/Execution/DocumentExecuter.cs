using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Introspection;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation;
using ExecutionContext = GraphQL.Execution.ExecutionContext;
using GraphQL.Resolvers;

namespace GraphQL
{
    public interface IDocumentExecuter
    {
        Task<ExecutionResult> ExecuteAsync(
            ISchema schema,
            object root,
            string query,
            string operationName,
            Inputs inputs = null,
            object userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null);
    }

    public class DocumentExecuter : IDocumentExecuter
    {
        private readonly IDocumentBuilder _documentBuilder;
        private readonly IDocumentValidator _documentValidator;

        public DocumentExecuter()
            : this(new GraphQLDocumentBuilder(), new DocumentValidator())
        {
        }

        public DocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator)
        {
            _documentBuilder = documentBuilder;
            _documentValidator = documentValidator;
        }

        public async Task<ExecutionResult> ExecuteAsync(
            ISchema schema,
            object root,
            string query,
            string operationName,
            Inputs inputs = null,
            object userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null)
        {
            var timings = new Timings();
            SchemaExtensions.Instrument(schema, timings);
            timings.Start(operationName);

            var result = new ExecutionResult();
            try
            {
                if (!schema.Initialized)
                {
                    using (timings.Subject("schema", "Initializing schema"))
                    {
                        schema.Initialize();
                    }
                }

                Document document;
                using (timings.Subject("document", "Building document"))
                {
                    document = _documentBuilder.Build(query);
                }

                IValidationResult validationResult;
                using (timings.Subject("document", "Validating document"))
                {
                    validationResult = _documentValidator.Validate(query, schema, document, rules);
                }

                if (validationResult.IsValid)
                {
                    var operation = GetOperation(operationName, document);

                    if (operation == null)
                    {
                        throw new ExecutionError("Unknown operation name: {0}".ToFormat(operationName));
                    }

                    timings.SetOperationName(operation.Name);

                    var context = BuildExecutionContext(schema, root, document, operation, inputs, userContext,
                        cancellationToken);

                    if (context.Errors.Any())
                    {
                        result.Errors = context.Errors;
                        return result;
                    }

                    using (timings.Subject("execution", "Executing operation"))
                    {
                        result.Data = await ExecuteOperationAsync(context).ConfigureAwait(false);
                    }

                    if (context.Errors.Any())
                    {
                        result.Errors = context.Errors;
                    }
                }
                else
                {
                    result.Data = null;
                    result.Errors = validationResult.Errors;
                }

                return result;
            }
            catch (Exception exc)
            {
                if (result.Errors == null)
                {
                    result.Errors = new ExecutionErrors();
                }

                result.Data = null;
                result.Errors.Add(new ExecutionError(exc.Message, exc));
                return result;
            }
            finally
            {
                result.Perf = timings.Finish().ToArray();
            }
        }

        public ExecutionContext BuildExecutionContext(
            ISchema schema,
            object root,
            Document document,
            Operation operation,
            Inputs inputs,
            object userContext,
            CancellationToken cancellationToken)
        {
            var context = new ExecutionContext();
            context.Document = document;
            context.Schema = schema;
            context.RootValue = root;
            context.UserContext = userContext;

            context.Operation = operation;
            context.Variables = GetVariableValues(document, schema, operation.Variables, inputs);
            context.Fragments = document.Fragments;
            context.CancellationToken = cancellationToken;

            return context;
        }

        private Operation GetOperation(string operationName, Document document)
        {
            var operation = !string.IsNullOrWhiteSpace(operationName)
                ? document.Operations.WithName(operationName)
                : document.Operations.FirstOrDefault();

            return operation;

        }

        public Task<Dictionary<string, object>> ExecuteOperationAsync(ExecutionContext context)
        {
            var rootType = GetOperationRootType(context.Document, context.Schema, context.Operation);
            var fields = CollectFields(
                context,
                rootType,
                context.Operation.SelectionSet,
                new Dictionary<string, Fields>(),
                new List<string>());

            return ExecuteFieldsAsync(context, rootType, context.RootValue, fields);
        }

        public Task<Dictionary<string, object>> ExecuteFieldsAsync(ExecutionContext context, IObjectGraphType rootType, object source, Dictionary<string, Fields> fields) {
            return fields.ToDictionaryAsync<KeyValuePair<string, Fields>, string, ResolveFieldResult<object>, object>(
                pair => pair.Key,
                pair => ResolveFieldAsync(context, rootType, source, pair.Value));
        }

        public async Task<ResolveFieldResult<object>> ResolveFieldAsync(ExecutionContext context, IObjectGraphType parentType, object source, Fields fields)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var resolveResult = new ResolveFieldResult<object>
            {
                Skip = false
            };

            var field = fields.First();

            var fieldDefinition = GetFieldDefinition(context.Schema, parentType, field);
            if (fieldDefinition == null)
            {
                resolveResult.Skip = true;
                return resolveResult;
            }

            var arguments = GetArgumentValues(context.Schema, fieldDefinition.Arguments, field.Arguments, context.Variables);

            try
            {
                var resolveContext = new ResolveFieldContext();
                resolveContext.FieldName = field.Name;
                resolveContext.FieldAst = field;
                resolveContext.FieldDefinition = fieldDefinition;
                resolveContext.ReturnType = fieldDefinition.ResolvedType;
                resolveContext.ParentType = parentType;
                resolveContext.Arguments = arguments;
                resolveContext.Source = source;
                resolveContext.Schema = context.Schema;
                resolveContext.Document = context.Document;
                resolveContext.Fragments = context.Fragments;
                resolveContext.RootValue = context.RootValue;
                resolveContext.UserContext = context.UserContext;
                resolveContext.Operation = context.Operation;
                resolveContext.Variables = context.Variables;
                resolveContext.CancellationToken = context.CancellationToken;
                var resolver = fieldDefinition.Resolver ?? new NameFieldResolver();
                var result = resolver.Resolve(resolveContext);

                if (result is Task)
                {
                    var task = result as Task;
                    await task.ConfigureAwait(false);

                    result = task.GetProperyValue("Result");
                }

                resolveResult.Value =
                    await CompleteValueAsync(context, fieldDefinition.ResolvedType, fields, result).ConfigureAwait(false);
                return resolveResult;
            }
            catch (Exception exc)
            {
                var error = new ExecutionError("Error trying to resolve {0}.".ToFormat(field.Name), exc);
                error.AddLocation(field, context.Document);
                context.Errors.Add(error);
                resolveResult.Skip = false;
                return resolveResult;
            }
        }

        public async Task<object> CompleteValueAsync(ExecutionContext context, IGraphType fieldType, Fields fields, object result)
        {
            var field = fields != null ? fields.FirstOrDefault() : null;
            var fieldName = field != null ? field.Name : null;

            var nonNullType = fieldType as NonNullGraphType;
            if (nonNullType != null)
            {
                var type = nonNullType.ResolvedType;
                var completed = await CompleteValueAsync(context, type, fields, result).ConfigureAwait(false);
                if (completed == null)
                {
                    var error = new ExecutionError("Cannot return null for non-null type. Field: {0}, Type: {1}!."
                        .ToFormat(fieldName, type.Name));
                    error.AddLocation(field, context.Document);
                    throw error;
                }

                return completed;
            }

            if (result == null)
            {
                return null;
            }

            if (fieldType is ScalarGraphType)
            {
                var scalarType = fieldType as ScalarGraphType;
                var coercedValue = scalarType.Serialize(result);
                return coercedValue;
            }

            if (fieldType is ListGraphType)
            {
                var list = result as IEnumerable;

                if (list == null)
                {
                    var error = new ExecutionError("User error: expected an IEnumerable list though did not find one.");
                    error.AddLocation(field, context.Document);
                    throw error;
                }

                var listType = fieldType as ListGraphType;
                var itemType = listType.ResolvedType;

                var results = await list.MapAsync(async item => await CompleteValueAsync(context, itemType, fields, item).ConfigureAwait(false)).ConfigureAwait(false);

                return results;
            }

            var objectType = fieldType as IObjectGraphType;

            if (fieldType is IAbstractGraphType)
            {
                var abstractType = fieldType as IAbstractGraphType;
                objectType = abstractType.GetObjectType(result);

                if (objectType != null && !abstractType.IsPossibleType(objectType))
                {
                    var error = new ExecutionError(
                        "Runtime Object type \"{0}\" is not a possible type for \"{1}\""
                        .ToFormat(objectType, abstractType));
                    error.AddLocation(field, context.Document);
                    throw error;
                }
            }

            if (objectType == null)
            {
                return null;
            }

            if (objectType.IsTypeOf != null && !objectType.IsTypeOf(result))
            {
                var error = new ExecutionError(
                    "Expected value of type \"{0}\" but got: {1}."
                    .ToFormat(objectType, result));
                error.AddLocation(field, context.Document);
                throw error;
            }

            var subFields = new Dictionary<string, Fields>();
            var visitedFragments = new List<string>();

            fields.Apply(f =>
            {
                subFields = CollectFields(context, objectType, f.SelectionSet, subFields, visitedFragments);
            });

            return await ExecuteFieldsAsync(context, objectType, result, subFields).ConfigureAwait(false);
        }

        public Dictionary<string, object> GetArgumentValues(ISchema schema, QueryArguments definitionArguments, Arguments astArguments, Variables variables)
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

        public FieldType GetFieldDefinition(ISchema schema, IObjectGraphType parentType, Field field)
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

            return parentType.Fields.FirstOrDefault(f => f.Name == field.Name);
        }

        public IObjectGraphType GetOperationRootType(Document document, ISchema schema, Operation operation)
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

        public Variables GetVariableValues(Document document, ISchema schema, VariableDefinitions variableDefinitions, Inputs inputs)
        {
            var variables = new Variables();
            variableDefinitions.Apply(v =>
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

        public object GetVariableValue(Document document, ISchema schema, VariableDefinition variable, object input)
        {
            var type = variable.Type.GraphTypeFromType(schema);

            if (IsValidValue(schema, type, input))
            {
                if (input == null)
                {
                    if (variable.DefaultValue != null)
                    {
                        return ValueFromAst(variable.DefaultValue);
                    }
                }
                var coercedValue = CoerceValue(schema, type, input.AstFromValue(schema, type));
                return coercedValue;
            }

            if (input == null)
            {
                var error2 = new ExecutionError("Variable '${0}' of required type '{1}' was not provided.".ToFormat(variable.Name, type.Name ?? variable.Type.FullName()));
                error2.AddLocation(variable, document);
                throw error2;
            }

            var error = new ExecutionError("Variable '${0}' expected value of type '{1}'.".ToFormat(variable.Name, type?.Name ?? variable.Type.FullName()));
            error.AddLocation(variable, document);
            throw error;
        }

        private object ValueFromAst(IValue value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is StringValue)
            {
                var str = (StringValue) value;
                return str.Value;
            }

            if (value is IntValue)
            {
                var num = (IntValue) value;
                return num.Value;
            }

            if (value is LongValue)
            {
                var num = (LongValue)value;
                return num.Value;
            }

            if (value is FloatValue)
            {
                var num = (FloatValue) value;
                return num.Value;
            }

            if (value is EnumValue)
            {
                var @enum = (EnumValue) value;
                return @enum.Name;
            }

            if (value is ObjectValue)
            {
                var objVal = (ObjectValue)value;
                var obj = new Dictionary<string, object>();
                objVal.FieldNames.Apply(name=>obj.Add(name, ValueFromAst(objVal.Field(name).Value)));
                return obj;
            }

            if (value is ListValue)
            {
                var list = (ListValue)value;
                return list.Values.Select(ValueFromAst).ToList();
            }

            return null;
        }

        public bool IsValidValue(ISchema schema, IGraphType type, object input)
        {
            if (type is NonNullGraphType)
            {
                if (input == null)
                {
                    return false;
                }

                var nonNullType = ((NonNullGraphType) type).ResolvedType;

                if (nonNullType is ScalarGraphType)
                {
                    var val = ValueFromScalar((ScalarGraphType) nonNullType, input);
                    return val != null;
                }

                return IsValidValue(schema, nonNullType, input);
            }

            if (input == null)
            {
                return true;
            }

            if (type is ListGraphType)
            {
                var listType = (ListGraphType) type;
                var listItemType = listType.ResolvedType;

                var list = input as IEnumerable;
                if (list != null && !(input is string))
                {
                    return list.All(item => IsValidValue(schema, listItemType, item));
                }

                return IsValidValue(schema, listItemType, input);
            }

            if (type is IObjectGraphType || type is InputObjectGraphType)
            {
                var dict = input as Dictionary<string, object>;
                var complexType = type as IComplexGraphType;

                if (dict == null)
                {
                    return false;
                }

                // ensure every provided field is defined
                if (type is InputObjectGraphType
                    && dict.Keys.Any(key => complexType.Fields.FirstOrDefault(field => field.Name == key) == null))
                {
                    return false;
                }

                return complexType.Fields.All(field =>
                {
                    object fieldValue = null;
                    dict.TryGetValue(field.Name, out fieldValue);
                    return IsValidValue(
                        schema,
                        field.ResolvedType,
                        fieldValue);
                });
            }

            if (type is ScalarGraphType)
            {
                var scalar = (ScalarGraphType) type;
                var value = ValueFromScalar(scalar, input);
                return value != null;
            }

            return false;
        }

        private object ValueFromScalar(ScalarGraphType scalar, object input)
        {
            if (input is IValue)
            {
                return scalar.ParseLiteral((IValue)input);
            }

            return scalar.ParseValue(input);
        }

        public object CoerceValue(ISchema schema, IGraphType type, IValue input, Variables variables = null)
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
                return variables != null
                    ? variables.ValueFor(variable.Name)
                    : null;
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

            if (type is IObjectGraphType || type is InputObjectGraphType)
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

        public Dictionary<string, Fields> CollectFields(
            ExecutionContext context,
            IGraphType specificType,
            SelectionSet selectionSet,
            Dictionary<string, Fields> fields,
            List<string> visitedFragmentNames)
        {
            if (fields == null)
            {
                fields = new Dictionary<string, Fields>();
            }

            selectionSet.Selections.Apply(selection =>
            {
                if (selection is Field)
                {
                    var field = (Field) selection;
                    if (!ShouldIncludeNode(context, field.Directives))
                    {
                        return;
                    }

                    var name = field.Alias ?? field.Name;
                    if (!fields.ContainsKey(name))
                    {
                        fields[name] = new Fields();
                    }
                    fields[name].Add(field);
                }
                else if (selection is FragmentSpread)
                {
                    var spread = (FragmentSpread) selection;

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

        public bool ShouldIncludeNode(ExecutionContext context, Directives directives)
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

        public bool DoesFragmentConditionMatch(ExecutionContext context, string fragmentName, IGraphType type)
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
                var abstractType = (IAbstractGraphType) conditionalType;
                return abstractType.IsPossibleType(type);
            }

            return false;
        }
    }
}
