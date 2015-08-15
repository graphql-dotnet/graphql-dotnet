using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Execution;
using GraphQL.Introspection;
using GraphQL.Language;
using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL
{
    public interface IDocumentExecuter
    {
        ExecutionResult Execute(Schema schema, object root, string query, string operationName, Inputs inputs = null);
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

        public ExecutionResult Execute(Schema schema, object root, string query, string operationName, Inputs inputs = null)
        {
            var document = _documentBuilder.Build(query);
            var result = new ExecutionResult();

            var validationResult = _documentValidator.IsValid(schema, document, operationName);

            if (validationResult.IsValid)
            {
                var context = BuildExecutionContext(schema, root, document, operationName, inputs);

                if (context.Errors.Any())
                {
                    result.Errors = context.Errors;
                    return result;
                }

                result.Data = ExecuteOperation(context);
                result.Errors = context.Errors;
            }
            else
            {
                result.Data = null;
                result.Errors = validationResult.Errors;
            }

            return result;
        }

        public ExecutionContext BuildExecutionContext(
            Schema schema,
            object root,
            Document document,
            string operationName,
            Inputs inputs)
        {
            var context = new ExecutionContext();
            context.Schema = schema;
            context.RootObject = root;

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

            return context;
        }

        public object ExecuteOperation(ExecutionContext context)
        {
            var rootType = GetOperationRootType(context.Schema, context.Operation);
            var fields = CollectFields(context, rootType, context.Operation.Selections, null);

            return ExecuteFields(context, rootType, context.RootObject, fields);
        }

        public object ExecuteFields(ExecutionContext context, ObjectGraphType rootType, object source, Dictionary<string, Fields> fields)
        {
            var result = new Dictionary<string, object>();

            fields.Apply(pair =>
            {
                result[pair.Key] = ResolveField(context, rootType, source, pair.Value);
            });

            return result;
        }

        public object ResolveField(ExecutionContext context, ObjectGraphType parentType, object source, Fields fields)
        {
            var field = fields.First();

            var fieldDefinition = GetFieldDefinition(context.Schema, parentType, field);
            if (fieldDefinition == null)
            {
                return null;
            }

            var arguments = GetArgumentValues(context.Schema, fieldDefinition.Arguments, field.Arguments, context.Variables);

            Func<ResolveFieldContext, object> defaultResolve = (ctx) =>
            {
                return ctx.Source != null ? GetProperyValue(ctx.Source, ctx.FieldAst.Name) : null;
            };

            try
            {
                var resolveContext = new ResolveFieldContext();
                resolveContext.FieldAst = field;
                resolveContext.FieldDefinition = fieldDefinition;
                resolveContext.Schema = context.Schema;
                resolveContext.ParentType = parentType;
                resolveContext.Arguments = arguments;
                resolveContext.Source = source;
                var resolve = fieldDefinition.Resolve ?? defaultResolve;
                var result = resolve(resolveContext);
                if (parentType is __Field && result is Type)
                {
                    result = context.Schema.FindType(result as Type);
                }
                return CompleteValue(context, context.Schema.FindType(fieldDefinition.Type), fields, result);
            }
            catch (Exception exc)
            {
                context.Errors.Add(new ExecutionError("Error trying to resolve {0}.".ToFormat(field.Name), exc));
                return null;
            }
        }

        public object GetProperyValue(object obj, string propertyName)
        {
            var val = obj.GetType()
                .GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                .GetValue(obj, null);

            return val;
        }

        public object CompleteValue(ExecutionContext context, GraphType fieldType, Fields fields, object result)
        {
            if (fieldType is NonNullGraphType)
            {
                var nonNullType = fieldType as NonNullGraphType;
                var completed = CompleteValue(context, context.Schema.FindType(nonNullType.Type), fields, result);
                if (completed == null)
                {
                    throw new ExecutionError("Cannot return null for non-null type. Field: {0}".ToFormat(nonNullType.Name));
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

                var results = list.Map(item =>
                {
                    return CompleteValue(context, itemType, fields, item);
                });

                return results;
            }

            var objectType = fieldType as ObjectGraphType;

            if (fieldType is InterfaceGraphType)
            {
                var interfaceType = fieldType as InterfaceGraphType;
                objectType = interfaceType.ResolveType(result);
            }

            if (objectType == null)
            {
                return null;
            }

            var subFields = new Dictionary<string, Fields>();

            fields.Apply(field =>
            {
                subFields = CollectFields(context, objectType, field.Selections, subFields);
            });

            return ExecuteFields(context, objectType, result, subFields);
        }

        public Dictionary<string, object> GetArgumentValues(Schema schema, QueryArguments definitionArguments, Arguments astArguments, Variables variables)
        {
            if (definitionArguments == null || !definitionArguments.Any())
            {
                return null;
            }

            return definitionArguments.Aggregate(new Dictionary<string, object>(), (acc, arg) =>
            {
                var value = astArguments != null ? astArguments.ValueFor(arg.Name) : null;
                var coercedValue = CoerceValueAst(schema, schema.FindType(arg.Type), value, variables);
                acc[arg.Name] = coercedValue ?? arg.DefaultValue;
                return acc;
            });
        }

        public FieldType GetFieldDefinition(Schema schema, ObjectGraphType parentType, Field field)
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

        public ObjectGraphType GetOperationRootType(Schema schema, Operation operation)
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

        public Variables GetVariableValues(Schema schema, Variables variables, Inputs inputs)
        {
            if (inputs != null)
            {
                variables.Apply(v =>
                {
                    v.Value = GetVariableValue(schema, v, inputs[v.Name]);
                });
            }

            return variables;
        }

        public object GetVariableValue(Schema schema, Variable variable, object input)
        {
            var type = schema.FindType(variable.Type.Name);
            if (IsValidValue(schema, type, input))
            {
                if (input == null && variable.DefaultValue != null)
                {
                    return CoerceValueAst(schema, type, variable.DefaultValue, null);
                }

                return CoerceValue(schema, type, input);
            }

            throw new Exception("Variable {0} expected type '{1}'.".ToFormat(variable.Name, type.Name));
        }

        public bool IsValidValue(Schema schema, GraphType type, object input)
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
                var list = input as IEnumerable;
                return list != null
                    ? list.All(item => IsValidValue(schema, type, item))
                    : IsValidValue(schema, listType, input);
            }

            if (type is ObjectGraphType)
            {
                var dict = input as Dictionary<string, object>;
                return dict != null
                    && type.Fields.All(field => IsValidValue(schema, schema.FindType(field.Type), dict[field.Name]));
            }

            if (type is ScalarGraphType)
            {
                var scalar = (ScalarGraphType) type;
                return scalar.Coerce(input) != null;
            }

            return false;
        }

        // TODO: combine dupliation with CoerceValueAST
        public object CoerceValue(Schema schema, GraphType type, object input)
        {
            if (type is NonNullGraphType)
            {
                var nonNull = type as NonNullGraphType;
                return CoerceValue(schema, schema.FindType(nonNull.Type), input);
            }

            if (input == null)
            {
                return null;
            }

            if (type is ListGraphType)
            {
                var listType = type as ListGraphType;
                var list = input as IEnumerable;
                return list != null
                    ? list.Map(item => CoerceValue(schema, listType, item))
                    : new[] { input };
            }

            if (type is ObjectGraphType)
            {
                var objType = type as ObjectGraphType;
                var obj = new Dictionary<string, object>();
                var dict = (Dictionary<string, object>)input;

                objType.Fields.Apply(field =>
                {
                    var fieldValue = CoerceValue(schema, schema.FindType(field.Type), dict[field.Name]);
                    obj[field.Name] = fieldValue ?? field.DefaultValue;
                });
            }

            if (type is ScalarGraphType)
            {
                var scalarType = type as ScalarGraphType;
                return scalarType.Coerce(input);
            }

            return null;
        }

        // TODO: combine duplication with CoerceValue
        public object CoerceValueAst(Schema schema, GraphType type, object input, Variables variables)
        {
            if (type is NonNullGraphType)
            {
                var nonNull = type as NonNullGraphType;
                return CoerceValueAst(schema, schema.FindType(nonNull.Type), input, variables);
            }

            if (input == null)
            {
                return null;
            }

            if (input is Variable)
            {
                return variables != null
                    ? variables.ValueFor(((Variable)input).Name)
                    : null;
            }

            if (type is ListGraphType)
            {
                var listType = type as ListGraphType;
                var list = input as IEnumerable;
                return list != null
                    ? list.Map(item => CoerceValueAst(schema, listType, item, variables))
                    : new[] { input };
            }

            if (type is ObjectGraphType)
            {
                var objType = type as ObjectGraphType;
                var obj = new Dictionary<string, object>();
                var dict = (Dictionary<string, object>)input;

                objType.Fields.Apply(field =>
                {
                    var fieldValue = CoerceValueAst(schema, schema.FindType(field.Type), dict[field.Name], variables);
                    obj[field.Name] = fieldValue ?? field.DefaultValue;
                });
            }

            if (type is ScalarGraphType)
            {
                var scalarType = type as ScalarGraphType;
                return scalarType.Coerce(input);
            }

            return input;
        }

        public Dictionary<string, Fields> CollectFields(ExecutionContext context, GraphType type, Selections selections, Dictionary<string, Fields> fields)
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

                        if (!ShouldIncludeNode(context, spread.Directives))
                        {
                            return;
                        }

                        var fragment = context.Fragments.FindDefinition(spread.Name);
                        if (!ShouldIncludeNode(context, fragment.Directives)
                            || !DoesFragmentConditionMatch(context, fragment, type))
                        {
                            return;
                        }

                        CollectFields(context, type, fragment.Selections, fields);
                    }
                    else if (selection.Fragment is InlineFragment)
                    {
                        var inline = selection.Fragment as InlineFragment;

                        if (!ShouldIncludeNode(context, inline.Directives)
                          || !DoesFragmentConditionMatch(context, inline, type))
                        {
                            return;
                        }

                        CollectFields(context, type, inline.Selections, fields);
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
            var conditionalType = context.Schema.FindType(fragment.Type);
            if (conditionalType == type)
            {
                return true;
            }

            if (conditionalType is InterfaceGraphType)
            {
                var interfaceType = (InterfaceGraphType) conditionalType;
                var hasInterfaces = type as IImplementInterfaces;
                if (hasInterfaces != null)
                {
                    var interfaces = context.Schema.FindTypes(hasInterfaces.Interfaces);
                    return interfaceType.IsPossibleType(interfaces);
                }
            }

            return false;
        }
    }
}
