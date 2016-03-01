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
            CancellationToken cancellationToken = default(CancellationToken));
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
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var document = _documentBuilder.Build(query);
            var result = new ExecutionResult();

            var validationResult = _documentValidator.IsValid(schema, document, operationName);

            if (validationResult.IsValid)
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
                context.Operation.Selections,
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

            Func<ResolveFieldContext, object> defaultResolve = (ctx) =>
            {
                return ctx.Source != null ? GetProperyValue(ctx.Source, ctx.FieldAst.Name) : null;
            };

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
                context.Errors.Add(new ExecutionError("Error trying to resolve {0}.".ToFormat(field.Name), exc));
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
            var nonNullType = fieldType as NonNullGraphType;
            if (nonNullType != null)
            {
                var type = context.Schema.FindType(nonNullType.Type);
                var completed = await CompleteValue(context, type, fields, result);
                if (completed == null)
                {
                    var field = fields != null ? fields.FirstOrDefault() : null;
                    var fieldName = field != null ? field.Name : null;
                    throw new ExecutionError("Cannot return null for non-null type. Field: {0}, Type: {1}!."
                        .ToFormat(fieldName, type.Name));
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
                var coercedValue = scalarType.Coerce(result);
                return coercedValue;
            }

            if (fieldType is ListGraphType)
            {
                var list = result as IEnumerable;

                if (list == null)
                {
                    throw new ExecutionError("User error: expected an IEnumerable list though did not find one.");
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
                    throw new ExecutionError(
                        "Runtime Object type \"{0}\" is not a possible type for \"{1}\""
                        .ToFormat(objectType, abstractType));
                }
            }

            if (objectType == null)
            {
                return null;
            }

            if (objectType.IsTypeOf != null && !objectType.IsTypeOf(result))
            {
                throw new ExecutionError(
                    "Expected value of type \"{0}\" but got: {1}."
                    .ToFormat(objectType, result));
            }

            var subFields = new Dictionary<string, Fields>();
            var visitedFragments = new List<string>();

            fields.Apply(field =>
            {
                subFields = CollectFields(context, objectType, field.Selections, subFields, visitedFragments);
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
                acc[arg.Name] = IsValidValue(schema, type, coercedValue)
                    ? coercedValue ?? arg.DefaultValue
                    : arg.DefaultValue;
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

                default:
                    throw new InvalidOperationException("Can only execute queries and mutations");
            }

            return type;
        }

        public Variables GetVariableValues(ISchema schema, Variables variables, Inputs inputs)
        {
            variables.Apply(v =>
            {
                object variableValue;
                if (inputs != null && inputs.TryGetValue(v.Name, out variableValue))
                {
                    v.Value = GetVariableValue(schema, v, variableValue);
                }
                else
                {
                    v.Value = GetVariableValue(schema, v, v.DefaultValue);
                }
            });
            return variables;
        }

        public object GetVariableValue(ISchema schema, Variable variable, object input)
        {
            var type = schema.FindType(variable.Type.FullName);

            if (type == null && !variable.Type.AllowsNull)
            {
                var nonNullType = new VariableType
                {
                    Name = variable.Type.Name,
                    IsList = variable.Type.IsList,
                    AllowsNull = true,
                };
                type = schema.FindType(nonNullType.FullName);
            }

            var value = input ?? variable.DefaultValue;
            if (IsValidValue(schema, type, value))
            {
                return CoerceValue(schema, type, value);
            }

            if (value == null)
            {
                throw new Exception("Variable '${0}' of required type '{1}' was not provided.".ToFormat(variable.Name, type.Name ?? variable.Type.FullName));
            }

            throw new Exception("Variable '${0}' expected value of type '{1}'.".ToFormat(variable.Name, type?.Name ?? variable.Type.FullName));
        }

        public bool IsValidValue(ISchema schema, GraphType type, object input)
        {
            if (type is NonNullGraphType)
            {
                if (input == null)
                {
                    return false;
                }

                return IsValidValue(schema, schema.FindType(((NonNullGraphType)type).Type), input);
            }

            if (input == null)
            {
                return true;
            }

            if (type is ListGraphType)
            {
                var listType = (ListGraphType) type;
                var listItemType = schema.FindType(listType.Type);
                var list = input as IEnumerable;
                return list != null && !(input is string)
                    ? list.All(item => IsValidValue(schema, listItemType, item))
                    : IsValidValue(schema, listItemType, input);
            }

            if (type is ObjectGraphType || type is InputObjectGraphType)
            {
                var dict = input as Dictionary<string, object>;
                if (dict == null)
                {
                    return false;
                }

                // ensure every provided field is defined
                if (type is InputObjectGraphType
                    && dict.Keys.Any(key => type.Fields.FirstOrDefault(field => field.Name == key) == null))
                {
                    return false;
                }

                return type.Fields.All(field =>
                           IsValidValue(schema, schema.FindType(field.Type),
                               dict.ContainsKey(field.Name) ? dict[field.Name] : null));
            }

            if (type is ScalarGraphType)
            {
                var scalar = (ScalarGraphType) type;
                return scalar.Coerce(input) != null;
            }

            return false;
        }

        public object CoerceValue(ISchema schema, GraphType type, object input, Variables variables = null)
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

            var variable = input as Variable;
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
                var list = input as IEnumerable;
                return list != null && !(input is string)
                    ? list.Map(item => CoerceValue(schema, listItemType, item, variables)).ToArray()
                    : new[] { CoerceValue(schema, listItemType, input, variables) };
            }

            if (type is ObjectGraphType || type is InputObjectGraphType)
            {
                var obj = new Dictionary<string, object>();

                if (input is KeyValuePair<string, object>)
                {
                    var kvp = (KeyValuePair<string, object>)input;
                    input = new Dictionary<string, object> { { kvp.Key, kvp.Value } };
                }

                var kvps = input as IEnumerable<KeyValuePair<string, object>>;
                if (kvps != null)
                {
                    input = kvps.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }

                var dict = input as Dictionary<string, object>;
                if (dict == null)
                {
                    return null;
                }

                type.Fields.Apply(field =>
                {
                    object inputValue;
                    if (dict.TryGetValue(field.Name, out inputValue))
                    {
                        var fieldValue = CoerceValue(schema, schema.FindType(field.Type), inputValue, variables);
                        obj[field.Name] = fieldValue ?? field.DefaultValue;
                    }
                });

                return obj;
            }

            if (type is ScalarGraphType)
            {
                var scalarType = type as ScalarGraphType;
                return scalarType.Coerce(input);
            }

            return input;
        }

        public Dictionary<string, Fields> CollectFields(
            ExecutionContext context,
            GraphType specificType,
            Selections selections,
            Dictionary<string, Fields> fields,
            List<string> visitedFragmentNames)
        {
            if (fields == null)
            {
                fields = new Dictionary<string, Fields>();
            }

            selections.Apply(selection =>
            {
                if (selection.Field != null)
                {
                    if (!ShouldIncludeNode(context, selection.Field.Directives))
                    {
                        return;
                    }

                    var name = selection.Field.Alias ?? selection.Field.Name;
                    if (!fields.ContainsKey(name))
                    {
                        fields[name] = new Fields();
                    }
                    fields[name].Add(selection.Field);
                }
                else if (selection.Fragment != null)
                {
                    if (selection.Fragment is FragmentSpread)
                    {
                        var spread = selection.Fragment as FragmentSpread;

                        if (visitedFragmentNames.Contains(spread.Name)
                            || !ShouldIncludeNode(context, spread.Directives))
                        {
                            return;
                        }

                        visitedFragmentNames.Add(spread.Name);

                        var fragment = context.Fragments.FindDefinition(spread.Name);
                        if (fragment == null
                            || !ShouldIncludeNode(context, fragment.Directives)
                            || !DoesFragmentConditionMatch(context, fragment, specificType))
                        {
                            return;
                        }

                        CollectFields(context, specificType, fragment.Selections, fields, visitedFragmentNames);
                    }
                    else if (selection.Fragment is InlineFragment)
                    {
                        var inline = selection.Fragment as InlineFragment;

                        if (!ShouldIncludeNode(context, inline.Directives)
                          || !DoesFragmentConditionMatch(context, inline, specificType))
                        {
                            return;
                        }

                        CollectFields(context, specificType, inline.Selections, fields, visitedFragmentNames);
                    }
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
                    return !((bool) values["if"]);
                }

                directive = directives.Find(DirectiveGraphType.Include.Name);
                if (directive != null)
                {
                    var values = GetArgumentValues(
                        context.Schema,
                        DirectiveGraphType.Include.Arguments,
                        directive.Arguments,
                        context.Variables);
                    return (bool) values["if"];
                }
            }

            return true;
        }

        public bool DoesFragmentConditionMatch(ExecutionContext context, IHaveFragmentType fragment, GraphType type)
        {
            if (string.IsNullOrWhiteSpace(fragment.Type))
            {
                return true;
            }

            var conditionalType = context.Schema.FindType(fragment.Type);

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
