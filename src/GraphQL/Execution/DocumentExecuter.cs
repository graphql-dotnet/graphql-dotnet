using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Introspection;
using GraphQL.Language;
using GraphQL.Types;
using GraphQL.Validation;
using ExecutionContext = GraphQL.Execution.ExecutionContext;

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
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null);
    }

    public class DocumentExecuter : IDocumentExecuter
    {
        private readonly IDocumentBuilder _documentBuilder;
        private readonly IDocumentValidator _documentValidator;

        public DocumentExecuter()
            : this(new AntlrDocumentBuilder(), new DocumentValidator())
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
            CancellationToken cancellationToken = default(CancellationToken),
            IEnumerable<IValidationRule> rules = null)
        {
            var document = _documentBuilder.Build(query);
            var result = new ExecutionResult();

            var validationResult = _documentValidator.Validate(schema, document, rules);

            if (validationResult.IsValid)
            {
                try
                {
                    var context = BuildExecutionContext(schema, root, document, operationName, inputs, cancellationToken);

                    if (context.Errors.Any())
                    {
                        result.Errors = context.Errors;
                        return result;
                    }

                    result.Data = await ExecuteOperation(context);
                    if (context.Errors.Any())
                    {
                        result.Errors = context.Errors;
                    }
                }
                catch (Exception exc)
                {
                    if (result.Errors == null)
                    {
                        result.Errors = new ExecutionErrors();
                    }

                    result.Data = null;
                    result.Errors.Add(new ExecutionError(exc.Message, exc));
                }
            }
            else
            {
                result.Data = null;
                result.Errors = validationResult.Errors;
            }

            return result;
        }

        public ExecutionContext BuildExecutionContext(
            ISchema schema,
            object root,
            Document document,
            string operationName,
            Inputs inputs,
            CancellationToken cancellationToken)
        {
            var context = new ExecutionContext();
            context.Schema = schema;
            context.RootValue = root;

            var operation = !string.IsNullOrWhiteSpace(operationName)
                ? document.Operations.WithName(operationName)
                : document.Operations.FirstOrDefault();

            if (operation == null)
            {
                context.Errors.Add(new ExecutionError("Unknown operation name: {0}".ToFormat(operationName)));
                return context;
            }

            context.Operation = operation;
            context.Variables = GetVariableValues(schema, operation.Variables, inputs);
            context.Fragments = document.Fragments;
            context.CancellationToken = cancellationToken;

            return context;
        }

        public Task<object> ExecuteOperation(ExecutionContext context)
        {
            var rootType = GetOperationRootType(context.Schema, context.Operation);
            var fields = CollectFields(
                context,
                rootType,
                context.Operation.SelectionSet,
                new Dictionary<string, Fields>(), 
                new List<string>());

            return ExecuteFields(context, rootType, context.RootValue, fields);
        }

        public async Task<object> ExecuteFields(ExecutionContext context, ObjectGraphType rootType, object source, Dictionary<string, Fields> fields)
        {
            return await fields.ToDictionaryAsync<KeyValuePair<string, Fields>,string, ResolveFieldResult<object>, object>(
                pair => pair.Key,
                pair => ResolveField(context, rootType, source, pair.Value));
        }

        public async Task<ResolveFieldResult<object>> ResolveField(ExecutionContext context, ObjectGraphType parentType, object source, Fields fields)
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

            Func<ResolveFieldContext, object> defaultResolve =
                ctx => ctx.Source != null
                    ? GetProperyValue(ctx.Source, ctx.FieldAst.Name)
                    : null;

            try
            {
                var resolveContext = new ResolveFieldContext();
                resolveContext.FieldName = field.Name;
                resolveContext.FieldAst = field;
                resolveContext.FieldDefinition = fieldDefinition;
                resolveContext.ReturnType = context.Schema.FindType(fieldDefinition.Type);
                resolveContext.ParentType = parentType;
                resolveContext.Arguments = arguments;
                resolveContext.Source = source;
                resolveContext.Schema = context.Schema;
                resolveContext.Fragments = context.Fragments;
                resolveContext.RootValue = context.RootValue;
                resolveContext.Operation = context.Operation;
                resolveContext.Variables = context.Variables;
                resolveContext.CancellationToken = context.CancellationToken;
                var resolve = fieldDefinition.Resolve ?? defaultResolve;
                var result = resolve(resolveContext);

                if(result is Task)
                {
                    var task = result as Task;
                    await task;

                    result = GetProperyValue(task, "Result");
                }

                if (parentType is __Field && result is Type)
                {
                    result = context.Schema.FindType(result as Type);
                }

                resolveResult.Value = await CompleteValue(context, context.Schema.FindType(fieldDefinition.Type), fields, result);
                return resolveResult;
            }
            catch (Exception exc)
            {
                var error = new ExecutionError("Error trying to resolve {0}.".ToFormat(field.Name), exc);
                error.AddLocation(field.SourceLocation.Line, field.SourceLocation.Column);
                context.Errors.Add(error);
                resolveResult.Skip = false;
                return resolveResult;
            }
        }

        public object GetProperyValue(object obj, string propertyName)
        {
            var val = obj.GetType()
                .GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                .GetValue(obj, null);

            return val;
        }

        public async Task<object> CompleteValue(ExecutionContext context, GraphType fieldType, Fields fields, object result)
        {
            var field = fields != null ? fields.FirstOrDefault() : null;
            var fieldName = field != null ? field.Name : null;

            var nonNullType = fieldType as NonNullGraphType;
            if (nonNullType != null)
            {
                var type = context.Schema.FindType(nonNullType.Type);
                var completed = await CompleteValue(context, type, fields, result);
                if (completed == null)
                {
                    var error = new ExecutionError("Cannot return null for non-null type. Field: {0}, Type: {1}!."
                        .ToFormat(fieldName, type.Name));
                    error.AddLocation(field);
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
                    error.AddLocation(field);
                    throw error;
                }

                var listType = fieldType as ListGraphType;
                var itemType = context.Schema.FindType(listType.Type);

                var results = await list.MapAsync(async item =>
                {
                    return await CompleteValue(context, itemType, fields, item);
                });

                return results;
            }

            var objectType = fieldType as ObjectGraphType;

            if (fieldType is GraphQLAbstractType)
            {
                var abstractType = fieldType as GraphQLAbstractType;
                objectType = abstractType.GetObjectType(result);

                if (objectType != null && !abstractType.IsPossibleType(objectType))
                {
                    var error = new ExecutionError(
                        "Runtime Object type \"{0}\" is not a possible type for \"{1}\""
                        .ToFormat(objectType, abstractType));
                    error.AddLocation(field);
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
                error.AddLocation(field);
                throw error;
            }

            var subFields = new Dictionary<string, Fields>();
            var visitedFragments = new List<string>();

            fields.Apply(f =>
            {
                subFields = CollectFields(context, objectType, f.SelectionSet, subFields, visitedFragments);
            });

            return await ExecuteFields(context, objectType, result, subFields);
        }

        public Dictionary<string, object> GetArgumentValues(ISchema schema, QueryArguments definitionArguments, Arguments astArguments, Variables variables)
        {
            if (definitionArguments == null || !definitionArguments.Any())
            {
                return null;
            }

            return definitionArguments.Aggregate(new Dictionary<string, object>(), (acc, arg) =>
            {
                var value = astArguments != null ? astArguments.ValueFor(arg.Name) : null;
                var type = schema.FindType(arg.Type);

                var coercedValue = CoerceValue(schema, type, value, variables);
                coercedValue = coercedValue ?? arg.DefaultValue;
                acc[arg.Name] = coercedValue;

                return acc;
            });
        }

        public FieldType GetFieldDefinition(ISchema schema, ObjectGraphType parentType, Field field)
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

        public ObjectGraphType GetOperationRootType(ISchema schema, Operation operation)
        {
            ObjectGraphType type;

            switch (operation.OperationType)
            {
                case OperationType.Query:
                    type = schema.Query;
                    break;

                case OperationType.Mutation:
                    type = schema.Mutation;
                    break;

                case OperationType.Subscription:
                    throw new ExecutionError("Subscriptions are not yet supported.");

                default:
                    throw new ExecutionError("Can only execute queries and mutations");
            }

            return type;
        }

        public Variables GetVariableValues(ISchema schema, VariableDefinitions variableDefinitions, Inputs inputs)
        {
            var variables = new Variables();
            variableDefinitions.Apply(v =>
            {
                var variable = new Variable();
                variable.Name = v.Name;

                object variableValue;
                if (inputs != null && inputs.TryGetValue(v.Name, out variableValue))
                {
                    var valueAst = AstFromValue(schema, variableValue, v.Type.GraphTypeFromType(schema));
                    variable.Value = GetVariableValue(schema, v, valueAst);
                }
                else
                {
                    variable.Value = GetVariableValue(schema, v, v.DefaultValue);
                }

                variables.Add(variable);
            });
            return variables;
        }

        public IValue AstFromValue(ISchema schema, object value, GraphType type)
        {
            if (type is NonNullGraphType)
            {
                var nonnull = (NonNullGraphType) type;
                return AstFromValue(schema, value, schema.FindType(nonnull.Type));
            }

            if (value is Dictionary<string, object>)
            {
                var dict = (Dictionary<string, object>) value;

                var fields = dict
                    .Select(pair => new ObjectField(pair.Key, AstFromValue(schema, pair.Value, null)))
                    .ToList();

                return new ObjectValue(fields);
            }

            if (!(value is string) && value is IEnumerable)
            {
                GraphType itemType = null;

                var listType = type as ListGraphType;
                if (listType != null)
                {
                    itemType = schema.FindType(listType.Type);
                }

                var list = (IEnumerable) value;
                var values = list.Map(item => AstFromValue(schema, item, itemType));
                return new ListValue(values);
            }

            if (value is bool)
            {
                return new BooleanValue((bool) value);
            }

            if (value is int)
            {
                return new IntValue((int) value);
            }

            if (value is long)
            {
                return new LongValue((long) value);
            }

            if (value is double)
            {
                return new FloatValue((double)value);
            }

            return new StringValue(value?.ToString());
        }

        public object GetVariableValue(ISchema schema, VariableDefinition variable, IValue input)
        {
            var type = schema.FindType(variable.Type.Name());

            var value = input ?? variable.DefaultValue;
            if (IsValidValue(schema, type, variable.Type, input))
            {
                var coercedValue = CoerceValue(schema, type, value);
                return coercedValue;
            }

            var val = ValueFromAst(value);

            if (val == null)
            {
                throw new ExecutionError("Variable '${0}' of required type '{1}' was not provided.".ToFormat(variable.Name, type.Name ?? variable.Type.FullName()));
            }

            throw new ExecutionError("Variable '${0}' expected value of type '{1}'.".ToFormat(variable.Name, type?.Name ?? variable.Type.FullName()));
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

            return null;
        }

        public bool IsValidValue(ISchema schema, GraphType type, IType astType, object input)
        {
            if (type is NonNullGraphType)
            {
                if (input == null)
                {
                    return false;
                }

                var nonNullType = schema.FindType(((NonNullGraphType) type).Type);

                if (nonNullType is ScalarGraphType)
                {
                    var val = ValueFromScalar((ScalarGraphType) nonNullType, input);
                    return val != null;
                }

                return IsValidValue(schema, nonNullType, astType, input);
            }

            if (astType is NonNullType)
            {
                if (input == null)
                {
                    return false;
                }

                if (type is ScalarGraphType)
                {
                    var val = ValueFromScalar((ScalarGraphType) type, input);
                    return val != null;
                }
            }

            if (input == null)
            {
                return true;
            }

            if (input is StringValue)
            {
                var stringVal = (StringValue) input;
                if (stringVal.Value == null)
                {
                    return true;
                }
            }

            if (type is ListGraphType)
            {
                var listType = (ListGraphType) type;
                var listItemType = schema.FindType(listType.Type);

                var list = input as IEnumerable;
                if (list != null && !(input is string))
                {
                    return list.All(item => IsValidValue(schema, listItemType, astType, item));
                }

                var listValue = input as ListValue;
                if (listValue != null)
                {
                    return listValue.Values.All(item => IsValidValue(schema, listItemType, astType, item));
                }

                return IsValidValue(schema, listItemType, astType, input);
            }

            if (type is ObjectGraphType || type is InputObjectGraphType)
            {
                var dict = input as ObjectValue;
                if (dict == null)
                {
                    return false;
                }

                // ensure every provided field is defined
                if (type is InputObjectGraphType
                    && dict.FieldNames.Any(key => type.Fields.FirstOrDefault(field => field.Name == key) == null))
                {
                    return false;
                }

                return type.Fields.All(field =>
                    IsValidValue(
                        schema,
                        schema.FindType(field.Type),
                        astType,
                        dict.Field(field.Name)?.Value));
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

        public object CoerceValue(ISchema schema, GraphType type, IValue input, Variables variables = null)
        {
            if (type is NonNullGraphType)
            {
                var nonNull = type as NonNullGraphType;
                return CoerceValue(schema, schema.FindType(nonNull.Type), input, variables);
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
                var listItemType = schema.FindType(listType.Type);
                var list = input as ListValue;
                return list != null
                    ? list.Values.Map(item => CoerceValue(schema, listItemType, item, variables)).ToArray()
                    : new[] { CoerceValue(schema, listItemType, input, variables) };
            }

            if (type is ObjectGraphType || type is InputObjectGraphType)
            {
                var obj = new Dictionary<string, object>();

                var objectValue = input as ObjectValue;
                if (objectValue == null)
                {
                    return null;
                }

                type.Fields.Apply(field =>
                {
                    var objectField = objectValue.Field(field.Name);
                    if (objectField != null)
                    {
                        var fieldValue = CoerceValue(schema, schema.FindType(field.Type), objectField.Value, variables);
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
            GraphType specificType,
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

                    if (!ShouldIncludeNode(context, inline.Directives)
                      || !DoesFragmentConditionMatch(context, inline.Type.Name, specificType))
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

        public bool DoesFragmentConditionMatch(ExecutionContext context, string fragmentName, GraphType type)
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

            if (conditionalType is GraphQLAbstractType)
            {
                var abstractType = (GraphQLAbstractType) conditionalType;
                return abstractType.IsPossibleType(type);
            }

            return false;
        }
    }
}
