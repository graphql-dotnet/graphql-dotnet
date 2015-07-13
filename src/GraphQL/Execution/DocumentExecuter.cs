using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Language;
using GraphQL.Validation;

namespace GraphQL
{
    public class GraphTypesLookup
    {
        private readonly Dictionary<string, GraphType> _types = new Dictionary<string, GraphType>();

        public GraphTypesLookup()
        {
            _types[ScalarGraphType.String.ToString()] = ScalarGraphType.String;
            _types[ScalarGraphType.Boolean.ToString()] = ScalarGraphType.Boolean;
            _types[ScalarGraphType.Float.ToString()] = ScalarGraphType.Float;
            _types[ScalarGraphType.Int.ToString()] = ScalarGraphType.Int;
            _types[ScalarGraphType.Id.ToString()] = ScalarGraphType.Id;

            _types[NonNullGraphType.String.ToString()] = NonNullGraphType.String;
            _types[NonNullGraphType.Boolean.ToString()] = NonNullGraphType.Boolean;
            _types[NonNullGraphType.Float.ToString()] = NonNullGraphType.Float;
            _types[NonNullGraphType.Int.ToString()] = NonNullGraphType.Int;
            _types[NonNullGraphType.Id.ToString()] = NonNullGraphType.Id;
        }

        public GraphType this[string typeName]
        {
            get
            {
                GraphType result = null;
                if (_types.ContainsKey(typeName))
                {
                    result = _types[typeName];
                }

                return result;
            }
            set { _types[typeName] = value; }
        }
    }

    public class Schema
    {
        private GraphTypesLookup _lookup;

        public ObjectGraphType Query { get; set; }

        public ObjectGraphType Mutation { get; set; }

        public GraphType FindType(string name)
        {
            if (_lookup == null)
            {
                _lookup = new GraphTypesLookup();
                CollectTypes(Query, _lookup);
                CollectTypes(Mutation, _lookup);
            }

            return _lookup[name];
        }

        private void CollectTypes(GraphType type, GraphTypesLookup lookup)
        {
            if (type == null)
            {
                return;
            }

            lookup[type.ToString()] = type;

            type.Fields.Apply(field =>
            {
                CollectTypes(field.Type, lookup);
            });
        }
    }

    public abstract class GraphType
    {
        private readonly List<FieldType> _fields = new List<FieldType>();

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<FieldType> Fields
        {
            get { return _fields; }
            private set
            {
                _fields.Clear();
                _fields.AddRange(value);
            }
        }

        public void Field(string name, string description, GraphType type, QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
        {
            if (_fields.Exists(x => x.Name == name))
            {
                throw new ArgumentOutOfRangeException("name", "A field with that name is already registered.");
            }

            _fields.Add(new FieldType
            {
                Name = name,
                Type = type,
                Arguments = arguments,
                Resolve = resolve
            });
        }

        public void Field(string name, GraphType type, QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
        {
            Field(name, null, type, arguments, resolve);
        }

        public void Field<TType>(string name, string description = null, QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
            where TType : GraphType, new()
        {
            Field(name, description, new TType(), arguments, resolve);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public abstract class ScalarGraphType : GraphType
    {
        public static StringGraphType String = new StringGraphType();
        public static BooleanGraphType Boolean = new BooleanGraphType();
        public static IntGraphType Int = new IntGraphType();
        public static FloatGraphType Float = new FloatGraphType();
        public static IdGraphType Id = new IdGraphType();

        public abstract object Coerce(object value);
    }

    public class StringGraphType : ScalarGraphType
    {
        public StringGraphType()
        {
            Name = "String";
        }

        public override object Coerce(object value)
        {
            return value != null ? value.ToString().Trim('"') : null;
        }
    }

    public class IntGraphType : ScalarGraphType
    {
        public IntGraphType()
        {
            Name = "Int";
        }

        public override object Coerce(object value)
        {
            int result;
            if (int.TryParse(value.ToString(), out result))
            {
                return result;
            }
            return null;
        }
    }

    public class FloatGraphType : ScalarGraphType
    {
        public FloatGraphType()
        {
            Name = "Float";
        }

        public override object Coerce(object value)
        {
            float result;
            if (float.TryParse(value.ToString(), out result))
            {
                return result;
            }
            return null;
        }
    }

    public class BooleanGraphType : ScalarGraphType
    {
        public BooleanGraphType()
        {
            Name = "Boolean";
        }

        public override object Coerce(object value)
        {
            if (value != null)
            {
                var stringValue = value.ToString().ToLower();
                switch (stringValue)
                {
                    case "false":
                    case "0":
                        return false;
                    case "true":
                    case "1":
                        return true;
                }
            }

            return null;
        }
    }

    public class IdGraphType : ScalarGraphType
    {
        public IdGraphType()
        {
            Name = "ID";
        }

        public override object Coerce(object value)
        {
            throw new NotImplementedException();
        }
    }

    public class NonNullGraphType : GraphType
    {
        public static readonly NonNullGraphType String = new NonNullGraphType(new StringGraphType());
        public static readonly NonNullGraphType Boolean = new NonNullGraphType(new BooleanGraphType());
        public static readonly NonNullGraphType Int = new NonNullGraphType(new IntGraphType());
        public static readonly NonNullGraphType Float = new NonNullGraphType(new FloatGraphType());
        public static readonly NonNullGraphType Id = new NonNullGraphType(new IdGraphType());

        public NonNullGraphType(GraphType type)
        {
            if (type.GetType() == typeof (NonNullGraphType))
            {
                throw new ArgumentException("Cannot nest NonNull inside NonNull.", "type");
            }

            Type = type;
        }

        public GraphType Type { get; private set; }

        public override string ToString()
        {
            return "{0}!".ToFormat(Type);
        }
    }

    public class EnumerationGraphType : ScalarGraphType
    {
        public override object Coerce(object value)
        {
            throw new NotImplementedException();
        }
    }

    public class InterfaceGraphType : ObjectGraphType
    {
        public Func<object, ObjectGraphType> ResolveType { get; set; }

        public bool IsPossibleType(IImplementInterfaces type)
        {
            return type != null
                ? type.Interfaces.Any(i => i.Name == this.Name)
                : false;
        }
    }

    public interface IImplementInterfaces
    {
        IEnumerable<InterfaceGraphType> Interfaces { get; }
    }

    public class ObjectGraphType : GraphType, IImplementInterfaces
    {
        private readonly List<InterfaceGraphType> _interfaces = new List<InterfaceGraphType>();

        public IEnumerable<InterfaceGraphType> Interfaces
        {
            get { return _interfaces; }
            set
            {
                _interfaces.Clear();
                _interfaces.AddRange(value);
            }
        }

        public void Interface<TInterface>()
            where TInterface : InterfaceGraphType, new()
        {
            _interfaces.Add(new TInterface());
        }
    }

    public class ListGraphType : GraphType
    {
        public ListGraphType(Type type)
        {
            Type = type;
        }

        public Type Type { get; private set; }

        public GraphType CreateType()
        {
            return (GraphType)Activator.CreateInstance(Type);
        }

        public override string ToString()
        {
            return "[{0}]".ToFormat(Type);
        }
    }

    public class ListGraphType<T> : ListGraphType
        where T : GraphType, new()
    {
        public ListGraphType()
            : base(typeof(T))
        {
        }
    }

    public class FieldType
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public object DefaultValue { get; set; }

        public GraphType Type { get; set; }

        public QueryArguments Arguments { get; set; }

        public Func<ResolveFieldContext, object> Resolve { get; set; }
    }

    public class ResolveArguments : List<ResolveArgument>
    {
    }

    public class ResolveArgument
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public class QueryArguments : List<QueryArgument>
    {
        public QueryArguments(IEnumerable<QueryArgument> list)
            : base(list)
        {
        }
    }

    public class QueryArgument
    {
        public string Name { get; set; }

        public GraphType Type { get; set; }
    }

    public class ExecutionResult
    {
        public object Data { get; set; }

        public ExecutionErrors Errors { get; set; }
    }

    public class ExecutionContext
    {
        public ExecutionContext()
        {
            Fragments = new Fragments();
            Errors = new ExecutionErrors();
        }

        public Schema Schema { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; }

        public Variables Variables { get; set; }

        public ExecutionErrors Errors { get; set; }
    }

    public class ExecutionErrors : IEnumerable<ExecutionError>
    {
        private readonly List<ExecutionError> _errors = new List<ExecutionError>();

        public void Add(ExecutionError error)
        {
            _errors.Add(error);
        }

        public IEnumerator<ExecutionError> GetEnumerator()
        {
            return _errors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ExecutionError : Exception
    {
        public ExecutionError(string message)
            : base(message)
        {
        }

        public ExecutionError(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class DocumentExecuter
    {
        public ExecutionResult Execute(Schema schema, string query, string operationName, Inputs inputs = null)
        {
            var documentBuilder = new GraphQLDocumentBuilder();
            var validator = new DocumentValidator();
            var document = documentBuilder.Build(query);
            var result = new ExecutionResult();

            var validationResult = validator.IsValid(schema, document, operationName);

            if (validationResult.IsValid)
            {
                var context = BuildExecutionContext(schema, document, operationName, inputs);

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
            Document document,
            string operationName,
            Inputs inputs)
        {
            var context = new ExecutionContext();
            context.Schema = schema;

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

            return ExecuteFields(context, rootType, null, fields);
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

            var arguments = GetArgumentValues(fieldDefinition.Arguments, field.Arguments, context.Variables);

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
                return CompleteValue(context, fieldDefinition.Type, fields, result);
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
                var completed = CompleteValue(context, nonNullType.Type, fields, result);
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

                var itemType = listType.CreateType();

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

        public Dictionary<string, object> GetArgumentValues(QueryArguments definitionArguments, Arguments astArguments, Variables variables)
        {
            if (definitionArguments == null || !definitionArguments.Any())
            {
                return null;
            }

            return definitionArguments.Aggregate(new Dictionary<string, object>(), (acc, arg) =>
            {
                var value = astArguments.ValueFor(arg.Name);
                acc[arg.Name] = CoerceValueAst(arg.Type, value, variables);
                return acc;
            });
        }

        public FieldType GetFieldDefinition(Schema schema, ObjectGraphType parentType, Field field)
        {
            // TODO: handle meta fields

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
            if (IsValidValue(type, input))
            {
                if (input == null && variable.DefaultValue != null)
                {
                    return CoerceValueAst(type, variable.DefaultValue, null);
                }

                return CoerceValue(type, input);
            }

            throw new Exception("Variable {0} expected type '{1}'.".ToFormat(variable.Name, type.Name));
        }

        public bool IsValidValue(GraphType type, object input)
        {
            if (type is NonNullGraphType)
            {
                if (input == null)
                {
                    return false;
                }

                return IsValidValue(((NonNullGraphType)type).Type, input);
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
                    ? list.All(item => IsValidValue(type, item))
                    : IsValidValue(listType, input);
            }

            if (type is ObjectGraphType)
            {
                var dict = input as Dictionary<string, object>;
                return dict != null
                    && type.Fields.All(field => IsValidValue(field.Type, dict[field.Name]));
            }

            if (type is ScalarGraphType)
            {
                var scalar = (ScalarGraphType) type;
                return scalar.Coerce(input) != null;
            }

            return false;
        }

        public object CoerceValue(GraphType type, object input)
        {
            if (type is NonNullGraphType)
            {
                var nonNull = type as NonNullGraphType;
                return CoerceValue(nonNull.Type, input);
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
                    ? list.Map(item => CoerceValue(listType, item))
                    : new[] { input };
            }

            if (type is ObjectGraphType)
            {
                var objType = type as ObjectGraphType;
                var obj = new Dictionary<string, object>();
                var dict = (Dictionary<string, object>)input;

                objType.Fields.Apply(field =>
                {
                    var fieldValue = CoerceValue(field.Type, dict[field.Name]);
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

        public object CoerceValueAst(GraphType type, object input, Variables variables)
        {
            if (type is NonNullGraphType)
            {
                var nonNull = type as NonNullGraphType;
                return CoerceValueAst(nonNull.Type, input, variables);
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
                    ? list.Map(item => CoerceValueAst(listType, item, variables))
                    : new[] { input };
            }

            if (type is ObjectGraphType)
            {
                var objType = type as ObjectGraphType;
                var obj = new Dictionary<string, object>();
                var dict = (Dictionary<string, object>)input;

                objType.Fields.Apply(field =>
                {
                    var fieldValue = CoerceValueAst(field.Type, dict[field.Name], variables);
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
                        var fragment = context.Fragments.FindDefinition(spread.Name);
                        if (DoesFragmentConditionMatch(context, fragment, type))
                        {
                            CollectFields(context, type, fragment.Selections, fields);
                        }
                    }
                    else if (selection.Fragment is InlineFragment)
                    {
                        var inline = selection.Fragment as InlineFragment;
                        if (DoesFragmentConditionMatch(context, inline, type))
                        {
                            CollectFields(context, type, inline.Selections, fields);
                        }
                    }
                }
            });

            return fields;
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
                return ((InterfaceGraphType)conditionalType).IsPossibleType(type as IImplementInterfaces);
            }

            return false;
        }
    }

    public class ResolveFieldContext
    {
        public Field FieldAst { get; set; }

        public FieldType FieldDefinition { get; set; }

        public ObjectGraphType ParentType { get; set; }

        public Dictionary<string, object> Arguments { get; set; }

        public object Source { get; set; }

        public Schema Schema { get; set; }
    }

    public class Inputs : Dictionary<string, object>
    {
    }
}
