using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Introspection;
using GraphQL.Language.AST;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using ExecutionContext = GraphQL.Execution.ExecutionContext;
using Field = GraphQL.Language.AST.Field;

namespace GraphQL
{
    public interface IDocumentExecuter
    {
        [Obsolete("This method will be removed in a future version.  Use ExecutionOptions parameter.")]
        Task<ExecutionResult> ExecuteAsync(
            ISchema schema,
            object root,
            string query,
            string operationName,
            Inputs inputs = null,
            object userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null);

        Task<ExecutionResult> ExecuteAsync(ExecutionOptions options);
        Task<ExecutionResult> ExecuteAsync(Action<ExecutionOptions> configure);
    }

    public class DocumentExecuter : IDocumentExecuter
    {
        private readonly IDocumentBuilder _documentBuilder;
        private readonly IDocumentValidator _documentValidator;
        private readonly IComplexityAnalyzer _complexityAnalyzer;

        public DocumentExecuter()
            : this(new GraphQLDocumentBuilder(), new DocumentValidator(), new ComplexityAnalyzer())
        {
        }

        public DocumentExecuter(IDocumentBuilder documentBuilder, IDocumentValidator documentValidator, IComplexityAnalyzer complexityAnalyzer)
        {
            _documentBuilder = documentBuilder;
            _documentValidator = documentValidator;
            _complexityAnalyzer = complexityAnalyzer;
        }

        [Obsolete("This method will be removed in a future version.  Use ExecutionOptions parameter.")]
        public Task<ExecutionResult> ExecuteAsync(
            ISchema schema,
            object root,
            string query,
            string operationName,
            Inputs inputs = null,
            object userContext = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null)
        {
            return ExecuteAsync(new ExecutionOptions
            {
                Schema = schema,
                Root = root,
                Query = query,
                OperationName = operationName,
                Inputs = inputs,
                UserContext = userContext,
                CancellationToken = cancellationToken,
                ValidationRules = rules
            });
        }

        public Task<ExecutionResult> ExecuteAsync(Action<ExecutionOptions> configure)
        {
            var options = new ExecutionOptions();
            configure(options);
            return ExecuteAsync(options);
        }

        protected virtual ExecutionResult InitializeResult(ExecutionOptions config)
        {
            var result = new ExecutionResult { Query = config.Query, ExposeExceptions = config.ExposeExceptions };
            return result;
        }

        protected virtual async Task OnExecution(ExecutionOptions config, ExecutionContext context, ExecutionResult result)
        {
            var task = ExecuteOperationAsync(context).ConfigureAwait(false);

            foreach (var listener in config.Listeners)
            {
                await listener.BeforeExecutionAwaitedAsync(config.UserContext, config.CancellationToken).ConfigureAwait(false);
            }

            result.Data = await task;
        }

        protected virtual void OnValidationError(IValidationResult validationResult, ExecutionResult result)
        {
            result.Data = null;
            result.Errors = validationResult.Errors;
        }

        protected virtual void OnError(Exception exception, ExecutionResult result)
        {
            if (result.Errors == null)
            {
                result.Errors = new ExecutionErrors();
            }

            result.Data = null;
            result.Errors.Add(new ExecutionError(exception.Message, exception));
        }

        private void ValidateOptions(ExecutionOptions options)
        {
            if (options.Schema == null)
            {
                throw new ExecutionError("A schema is required.");
            }

            if (string.IsNullOrWhiteSpace(options.Query))
            {
                throw new ExecutionError("A query is required.");
            }
        }

        public async Task<ExecutionResult> ExecuteAsync(ExecutionOptions config)
        {
            var metrics = new Metrics(config.EnableMetrics);
            metrics.Start(config.OperationName);

            config.Schema.FieldNameConverter = config.FieldNameConverter;

            var result = InitializeResult(config);
            try
            {
                ValidateOptions(config);

                if (!config.Schema.Initialized)
                {
                    using (metrics.Subject("schema", "Initializing schema"))
                    {
                        if (config.SetFieldMiddleware)
                        {
                            config.FieldMiddleware.ApplyTo(config.Schema);
                        }
                        config.Schema.Initialize();
                    }
                }

                var document = config.Document;
                using (metrics.Subject("document", "Building document"))
                {
                    if (document == null)
                    {
                        document = _documentBuilder.Build(config.Query);
                    }
                }

                result.Document = document;

                var operation = GetOperation(config.OperationName, document);
                result.Operation = operation;
                metrics.SetOperationName(operation?.Name);

                if (operation == null)
                {
                    throw new ExecutionError("Unable to determine operation from query.");
                }

                IValidationResult validationResult;
                using (metrics.Subject("document", "Validating document"))
                {
                    validationResult = _documentValidator.Validate(
                        config.Query,
                        config.Schema,
                        document,
                        config.ValidationRules,
                        config.UserContext);
                }

                if (config.ComplexityConfiguration != null && validationResult.IsValid)
                {
                    using (metrics.Subject("document", "Analyzing complexity"))
                        _complexityAnalyzer.Validate(document, config.ComplexityConfiguration);
                }

                foreach (var listener in config.Listeners)
                {
                    await listener.AfterValidationAsync(
                            config.UserContext,
                            validationResult,
                            config.CancellationToken)
                        .ConfigureAwait(false);
                }

                if (validationResult.IsValid)
                {
                    var context = BuildExecutionContext(
                        config.Schema,
                        config.Root,
                        document,
                        operation,
                        config.Inputs,
                        config.UserContext,
                        config.CancellationToken,
                        metrics);

                    if (context.Errors.Any())
                    {
                        result.Errors = context.Errors;
                        return result;
                    }

                    using (metrics.Subject("execution", "Executing operation"))
                    {
                        foreach (var listener in config.Listeners)
                        {
                            await listener.BeforeExecutionAsync(config.UserContext, config.CancellationToken).ConfigureAwait(false);
                        }

                        await OnExecution(config, context, result);

                        foreach (var listener in config.Listeners)
                        {
                            await listener.AfterExecutionAsync(config.UserContext, config.CancellationToken).ConfigureAwait(false);
                        }
                    }

                    if (context.Errors.Any())
                    {
                        result.Errors = context.Errors;
                    }
                }
                else
                {
                    OnValidationError(validationResult, result);
                }

                return result;
            }
            catch (Exception exc)
            {
                OnError(exc, result);
                return result;
            }
            finally
            {
                result.Perf = metrics.Finish()?.ToArray();
            }
        }

        public ExecutionContext BuildExecutionContext(
            ISchema schema,
            object root,
            Document document,
            Operation operation,
            Inputs inputs,
            object userContext,
            CancellationToken cancellationToken,
            Metrics metrics)
        {
            var context = new ExecutionContext();
            context.Document = document;
            context.Schema = schema;
            context.RootValue = root;
            context.UserContext = userContext;

            context.Operation = operation;
            context.Variables = GetVariableValues(document, schema, operation?.Variables, inputs);
            context.Fragments = document.Fragments;
            context.CancellationToken = cancellationToken;

            context.Metrics = metrics;

            return context;
        }

        protected virtual Operation GetOperation(string operationName, Document document)
        {
            var operation = !string.IsNullOrWhiteSpace(operationName)
                ? document.Operations.WithName(operationName)
                : document.Operations.FirstOrDefault();

            return operation;
        }

        public Task<IDictionary<string, object>> ExecuteOperationAsync(ExecutionContext context)
        {
            var rootType = GetOperationRootType(context.Document, context.Schema, context.Operation);
            var fields = CollectFields(
                context,
                rootType,
                context.Operation.SelectionSet,
                new Dictionary<string, Field>(),
                new List<string>());

            return ExecuteFieldsAsync(context, rootType, context.RootValue, fields, new string[0]);
        }

        public async Task<IDictionary<string, object>> ExecuteFieldsAsync(ExecutionContext context, IObjectGraphType rootType, object source, Dictionary<string, Field> fields, IEnumerable<string> path)
        {

            var data = new ConcurrentDictionary<string, object>();
            var externalTasks = new List<Task>();

            foreach (var fieldCollection in fields)
            {
                var currentPath = path.Concat(new[] { fieldCollection.Key });

                var field = fieldCollection.Value;
                var fieldType = GetFieldDefinition(context.Document, context.Schema, rootType, field);

                if (fieldType?.Resolver == null || !fieldType.Resolver.RunThreaded() || context.Operation.OperationType == OperationType.Mutation)
                {
                    await ExtractFieldAsync(context, rootType, source, field, fieldType, data, currentPath);
                }
                else
                {
                    var task = Task.Run(() => ExtractFieldAsync(context, rootType, source, field, fieldType, data, currentPath));

                    externalTasks.Add(task);
                }
            }

            if (externalTasks.Count > 0)
            {
                Task.WaitAll(externalTasks.ToArray());
            }

            var ordered = new Dictionary<string, object>();

            foreach (var fieldCollection in fields)
            {
                var name = fieldCollection.Key; 

                if (!data.ContainsKey(name))
                {
                    continue;
                }

                ordered.Add(name, data[name]);
            }

            return ordered;
        }

        private async Task ExtractFieldAsync(ExecutionContext context, IObjectGraphType rootType, object source,
            Field field, FieldType fieldType, ConcurrentDictionary<string, object> data, IEnumerable<string> path)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var name = field.Alias ?? field.Name;

            if (data.ContainsKey(name))
            {
                return;
            }

            if (!ShouldIncludeNode(context, field.Directives))
            {
                return;
            }

            if (CanResolveFromData(field, fieldType))
            {
                var result = ResolveFieldFromData(context, rootType, source, fieldType, field, path);

                data.TryAdd(name, result);
            }
            else
            {
                var result = await ResolveFieldAsync(context, rootType, source, field, path);

                if (result.Skip)
                {
                    return;
                }

                data.TryAdd(name, result.Value);
            }
        }

        private bool CanResolveFromData(Field field, FieldType type)
        {
            if (field == null || type == null)
            {
                return false;
            }

            if (type.Arguments != null &&
                type.Arguments.Any())
            {
                return false;
            }

            if (!(type.ResolvedType is ScalarGraphType))
            {
                return false;
            }

            if (type.ResolvedType is NonNullGraphType)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Resolve simple fields in a performant manor
        /// </summary>
        private static object ResolveFieldFromData(ExecutionContext context, IObjectGraphType rootType, object source,
            FieldType fieldType, Field field, IEnumerable<string> path)
        {
            object result = null;

            try
            {
                if (fieldType.Resolver != null)
                {
                    var rfc = new ResolveFieldContext(context, field, fieldType, source, rootType, null, path);

                    result = fieldType.Resolver.Resolve(rfc);
                }
                else
                {
                    result = NameFieldResolver.Resolve(source, field.Name);
                }

                if (result != null)
                {
                    var scalarType = fieldType.ResolvedType as ScalarGraphType;

                    result = scalarType?.Serialize(result);
                }
            }
            catch (Exception exc)
            {
                var error = new ExecutionError($"Error trying to resolve {field.Name}.", exc);
                error.AddLocation(field, context.Document);
                error.Path = path.ToList();
                context.Errors.Add(error);
            }

            return result;
        }

        public async Task<ResolveFieldResult<object>> ResolveFieldAsync(ExecutionContext context, IObjectGraphType parentType, object source, Field field, IEnumerable<string> path)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var fieldPath = path?.ToList() ?? new List<string>();

            var resolveResult = new ResolveFieldResult<object>
            {
                Skip = false
            };

            var fieldDefinition = GetFieldDefinition(context.Document, context.Schema, parentType, field);
            if (fieldDefinition == null)
            {
                resolveResult.Skip = true;
                return resolveResult;
            }

            var arguments = GetArgumentValues(context.Schema, fieldDefinition.Arguments, field.Arguments, context.Variables);

            try
            {
                var resolveContext = new ResolveFieldContext
                {
                    FieldName = field.Name,
                    FieldAst = field,
                    FieldDefinition = fieldDefinition,
                    ReturnType = fieldDefinition.ResolvedType,
                    ParentType = parentType,
                    Arguments = arguments,
                    Source = source,
                    Schema = context.Schema,
                    Document = context.Document,
                    Fragments = context.Fragments,
                    RootValue = context.RootValue,
                    UserContext = context.UserContext,
                    Operation = context.Operation,
                    Variables = context.Variables,
                    CancellationToken = context.CancellationToken,
                    Metrics = context.Metrics,
                    Errors = context.Errors,
                    Path = fieldPath
                };

                var subFields = SubFieldsFor(context, fieldDefinition.ResolvedType, field);
                resolveContext.SubFields = subFields;

                var resolver = fieldDefinition.Resolver ?? new NameFieldResolver();
                var result = resolver.Resolve(resolveContext);

                if (result is Task)
                {
                    var task = result as Task;
                    if (task.IsFaulted)
                    {
                        var aggregateException = task.Exception;
                        var exception = aggregateException.InnerExceptions.Count == 1
                            ? aggregateException.InnerException
                            : aggregateException;
                        return GenerateError(resolveResult, field, context, exception, fieldPath);
                    }
                    await task.ConfigureAwait(false);

                    result = task.GetProperyValue("Result");
                }

                resolveResult.Value =
                    await CompleteValueAsync(context, parentType, fieldDefinition.ResolvedType, field, result, fieldPath).ConfigureAwait(false);
                return resolveResult;
            }
            catch (Exception exc)
            {
                return GenerateError(resolveResult, field, context, exc, path);
            }
        }

        private IDictionary<string, Field> SubFieldsFor(ExecutionContext context, IGraphType fieldType, Field field)
        {
            if (!(fieldType is IObjectGraphType) || !field.SelectionSet.Selections.Any())
            {
                return null;
            }

            var subFields = new Dictionary<string, Field>();
            var visitedFragments = new List<string>();
            var fields = CollectFields(context, fieldType, field.SelectionSet, subFields, visitedFragments);
            return fields;
        }

        private ResolveFieldResult<object> GenerateError(
            ResolveFieldResult<object> resolveResult,
            Field field,
            ExecutionContext context,
            Exception exc,
            IEnumerable<string> path)
        {
            var error = new ExecutionError("Error trying to resolve {0}.".ToFormat(field.Name), exc);
            error.AddLocation(field, context.Document);
            error.Path = path;
            context.Errors.Add(error);
            resolveResult.Skip = false;
            return resolveResult;
        }

        public async Task<object> CompleteValueAsync(ExecutionContext context, IObjectGraphType parentType, IGraphType fieldType, Field field, object result, IEnumerable<string> path)
        {
            var fieldName = field?.Name;

            var nonNullType = fieldType as NonNullGraphType;
            if (nonNullType != null)
            {
                var type = nonNullType.ResolvedType;
                var completed = await CompleteValueAsync(context, parentType, type, field, result, path).ConfigureAwait(false);
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
                var results = await ResolveListFromData(context, result, parentType, fieldType, field, path);

                return results;
            }

            var objectType = fieldType as IObjectGraphType;

            if (fieldType is IAbstractGraphType)
            {
                var abstractType = fieldType as IAbstractGraphType;
                objectType = abstractType.GetObjectType(result);

                if (objectType == null)
                {
                    var error = new ExecutionError(
                        $"Abstract type {abstractType.Name} must resolve to an Object type at " +
                        $"runtime for field {parentType.Name}.{fieldName} " +
                        $"with value {result}, received 'null'.");
                    error.AddLocation(field, context.Document);
                    throw error;
                }

                if (!abstractType.IsPossibleType(objectType))
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

            var subFields = new Dictionary<string, Field>();
            var visitedFragments = new List<string>();

            subFields = CollectFields(context, objectType, field?.SelectionSet, subFields, visitedFragments);

            return await ExecuteFieldsAsync(context, objectType, result, subFields, path).ConfigureAwait(false);
        }

        /// <summary>
        ///     Resolve lists in a performant manor
        /// </summary>
        private async Task<List<object>> ResolveListFromData(ExecutionContext context, object source, IObjectGraphType parentType,
            IGraphType graphType, Field field, IEnumerable<string> path)
        {
            var result = new List<object>();
            var listInfo = graphType as ListGraphType;
            var subType = listInfo?.ResolvedType as IObjectGraphType;
            var data = source as IEnumerable;
            var visitedFragments = new List<string>();
            var subFields = CollectFields(context, subType, field.SelectionSet, null, visitedFragments);

            if (data == null)
            {
                var error = new ExecutionError("User error: expected an IEnumerable list though did not find one.");
                error.AddLocation(field, context.Document);
                throw error;
            }

            var index = 0;
            foreach (var node in data)
            {
                var currentPath = path.Concat(new[] {$"{index++}"});

                if (subType != null)
                {
                    var nodeResult = await ExecuteFieldsAsync(context, subType, node, subFields, currentPath);

                    result.Add(nodeResult);
                }
                else
                {
                    var nodeResult = await CompleteValueAsync(context, parentType, listInfo?.ResolvedType, field, node, currentPath).ConfigureAwait(false);

                    result.Add(nodeResult);
                }
            }

            return result;
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

        public FieldType GetFieldDefinition(Document document, ISchema schema, IObjectGraphType parentType, Field field)
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

        public object GetVariableValue(Document document, ISchema schema, VariableDefinition variable, object input)
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
                    return variable.DefaultValue.ValueFromAst();
                }
            }
            var coercedValue = CoerceValue(schema, type, input.AstFromValue(schema, type));
            return coercedValue;
        }

        public void AssertValidValue(ISchema schema, IGraphType type, object input, string fieldName)
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

        public Dictionary<string, Field> CollectFields(
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
                var abstractType = (IAbstractGraphType)conditionalType;
                return abstractType.IsPossibleType(type);
            }

            return false;
        }
    }
}
