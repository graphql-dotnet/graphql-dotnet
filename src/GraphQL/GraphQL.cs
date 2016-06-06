using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Language;
using GraphQL.Types;
using Newtonsoft.Json.Serialization;
using ExecutionContext = GraphQL.Execution.ExecutionContext;

namespace GraphQL2
{
    public interface IGraphQLType
    {
        string Name { get; set; }
    }

    public interface IGraphQLOutputType : IGraphQLType
    {
    }

    public interface IGraphQLInputType : IGraphQLType
    {
    }

    public abstract class GraphQLType : IGraphQLType
    {
        internal GraphQLType()
        {
        }

        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class GraphQLScalarTypeConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public Func<object, object> Serialize { get; set; }
        public Func<object, object> ParseValue { get; set; }
        public Func<IValue, object> ParseLiteral { get; set; }
    }

    public class GraphQLScalarTypeConfig<T> : GraphQLScalarTypeConfig
    {
        public new Func<object, T> ParseValue { get; set; }
        public new Func<IValue, T> ParseLiteral { get; set; }
    }

    public class GraphQLScalarType : GraphQLType, IGraphQLInputType, IGraphQLOutputType
    {
        protected GraphQLScalarType(GraphQLScalarTypeConfig config)
        {
            Name = config.Name;
            Description = config.Description;
            DeprecationReason = config.DeprecationReason;
        }

        public string Description { get; protected set; }
        public string DeprecationReason { get; protected set; }

        public static GraphQLScalarType For(Action<GraphQLScalarTypeConfig> configure)
        {
            var config = new GraphQLScalarTypeConfig();
            configure(config);
            return new GraphQLScalarType(config);
        }
    }

    public class GraphQLScalarType<TType> : GraphQLScalarType
    {
        public GraphQLScalarType(GraphQLScalarTypeConfig config)
            : base(config)
        {
        }

        public static GraphQLScalarType<TType> For(Action<GraphQLScalarTypeConfig<TType>> configure)
        {
            var config = new GraphQLScalarTypeConfig<TType>();
            configure(config);
            return new GraphQLScalarType<TType>(config);
        }
    }

    public class GraphQLEnumType : GraphQLScalarType
    {
        private readonly List<EnumValueDefinition> _values = new List<EnumValueDefinition>();

        public GraphQLEnumType(GraphQLEnumTypeConfig config)
            : base(config)
        {
            _values.AddRange(config.Values);
        }

        public IEnumerable<EnumValueDefinition> Values => _values;

        public static GraphQLEnumType For<T>(Action<GraphQLEnumTypeConfig<T>> configure)
        {
            var config = new GraphQLEnumTypeConfig<T>();
            configure(config);
            return new GraphQLEnumType(config);
        }

        public static GraphQLEnumType For<T>(string description = null)
        {
            var type = typeof(T);

            Invariant.Check(type.IsEnum, $"{type.Name} must be of type enum.");

            var config = new GraphQLEnumTypeConfig<T>();
            config.Name = type.Name.PascalCase();
            config.Description = description;

            foreach (var enumName in type.GetEnumNames())
            {
                var enumMember = type
                  .GetMember(enumName, BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                  .First();

                var name = DeriveEnumValueName(enumMember.Name);

                config.Value(name, (T)Enum.Parse(type, enumName));
            }

            return new GraphQLEnumType(config);
        }

        static string DeriveEnumValueName(string name)
        {
            return Regex
              .Replace(name, @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4")
              .ToUpperInvariant();
        }
    }

    public static class GraphQLScalarTypes
    {
        public static GraphQLScalarType GraphQLInt = GraphQLScalarType.For(_ =>
        {
            _.Name = "Int";
        });

        public static GraphQLScalarType<double> GraphQLFloat = GraphQLScalarType<double>.For(_ =>
        {
            _.Name = "Float";
        });

        public static GraphQLScalarType<string> GraphQLString = GraphQLScalarType<string>.For(_ =>
        {
            _.Name = "String";
        });

        public static GraphQLScalarType<bool> GraphQLBoolean = GraphQLScalarType<bool>.For(_ =>
        {
            _.Name = "Boolean";
        });

        public static GraphQLScalarType GraphQLID = GraphQLScalarType.For(_ =>
        {
            _.Name = "ID";
        });
    }

    public class InputObjectField
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public object DefaultValue { get; set; }
        public IGraphQLInputType Type { get; set; }
    }

    public class GraphQLInputObjectTypeConfig
    {
        private readonly List<InputObjectField> _fields = new List<InputObjectField>();

        public string Name { get; set; }
        public string Description { get; set; }

        public IEnumerable<InputObjectField> Fields => _fields;

        public void Field(InputObjectField field)
        {
            _fields.Add(field);
        }

        public void Field(Action<InputObjectField> configure)
        {
            var field = new InputObjectField();
            configure(field);
            _fields.Add(field);
        }

        public void Field(
            string name,
            IGraphQLInputType type,
            string description = null,
            object defaultValue = null)
        {
            _fields.Add(new InputObjectField
            {
                Name = name,
                Type = type,
                Description = description,
                DefaultValue = defaultValue
            });
        }

        public void Field<TProperty>(
            string name,
            IGraphQLInputType type,
            string description = null,
            TProperty defaultValue = default(TProperty))
        {
            _fields.Add(new InputObjectField
            {
                Name = name,
                Type = type,
                Description = description,
                DefaultValue = defaultValue
            });
        }

        public void Field<TProperty>(
            string name,
            string description = null,
            TProperty defaultValue = default(TProperty))
        {
            var type = (IGraphQLInputType)typeof(TProperty).GetGraphQLTypeFromType();
            Field(name, type, description, defaultValue);
        }
    }

    public class GraphQLInputObjectTypeConfig<T> : GraphQLInputObjectTypeConfig
    {
        public void Field<TProperty>(
            Expression<Func<T, TProperty>> name,
            string description = null,
            TProperty defaultValue = default(TProperty))
        {
            var fieldName = name.NameOf();
            Field(fieldName, description, defaultValue);
        }
    }

    public class GraphQLInputObjectType : GraphQLType, IGraphQLInputType
    {
        private readonly List<InputObjectField> _fields = new List<InputObjectField>();

        public GraphQLInputObjectType(GraphQLInputObjectTypeConfig config)
        {
            Name = config.Name;
            Description = config.Description;
            _fields.AddRange(config.Fields);
        }

        public string Description { get; set; }

        public IEnumerable<InputObjectField> Fields => _fields;

        public static GraphQLInputObjectType For(Action<GraphQLInputObjectTypeConfig> configure)
        {
            var config = new GraphQLInputObjectTypeConfig();
            configure(config);
            return new GraphQLInputObjectType(config);
        }

        public static GraphQLInputObjectType For<T>(Action<GraphQLInputObjectTypeConfig<T>> configure)
        {
            var config = new GraphQLInputObjectTypeConfig<T>();
            configure(config);
            return new GraphQLInputObjectType(config);
        }
    }

    public class GraphQLFieldDefinition
    {
        public GraphQLFieldDefinition()
        {
            Arguments = new GraphQLArgument[0];
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public IGraphQLOutputType Type { get; set; }
        public IEnumerable<GraphQLArgument> Arguments { get; set; }
        public IFieldResolver Resolver { get; set; }
    }

    public class GraphQLEnumTypeConfig : GraphQLScalarTypeConfig
    {
        private readonly List<EnumValueDefinition> _values = new List<EnumValueDefinition>();

        public GraphQLEnumTypeConfig()
        {
            Serialize = value =>
            {
                var found = _values.FirstOrDefault(v => v.Value.Equals(value));
                return found?.Name;
            };

            ParseValue = value =>
            {
                var found = _values.FirstOrDefault(v =>
                    StringComparer.InvariantCultureIgnoreCase.Equals(v.Name, value.ToString()));
                return found?.Value;
            };

            ParseLiteral = value =>
            {
                var enumVal = value as EnumValue;
                return ParseValue(enumVal?.Name);
            };
        }

        public IEnumerable<EnumValueDefinition> Values => _values;

        protected void AddValue(EnumValueDefinition definition)
        {
            _values.Add(definition);
        }
    }

    public class GraphQLEnumTypeConfig<T> : GraphQLEnumTypeConfig
    {
        public void Value(string name, T value, string description = null, string deprecationReason = null)
        {
            var def = new EnumValueDefinition
            {
                Name = name,
                Value = value,
                Description = description,
                DeprecationReason = deprecationReason
            };
            AddValue(def);
        }
    }

    public class GraphQLTypeFieldConfig
    {
        private readonly List<GraphQLFieldDefinition> _fields = new List<GraphQLFieldDefinition>();
        public IEnumerable<GraphQLFieldDefinition> Fields => _fields;

        public void Field(GraphQLFieldDefinition field)
        {
            _fields.Add(field);
        }

        public void Field(Action<GraphQLFieldConfig> fieldConfig)
        {
            var config = new GraphQLFieldConfig();
            fieldConfig(config);

            var def = new GraphQLFieldDefinition
            {
                Name = config.Name,
                Type = config.Type,
                Description = config.Description,
                DeprecationReason = config.DeprecationReason,
                Resolver = config.Resolve,
                Arguments = config.Arguments
            };

            _fields.Add(def);
        }

        public void Field<T>(
            string name,
            IGraphQLOutputType type,
            string description = null,
            string deprecationReason = null,
            Func<ResolveFieldContext, T> resolve = null,
            Action<GraphQLArgumentConfig> args = null)
        {
            var def = new GraphQLFieldDefinition
            {
                Name = name,
                Description = description,
                DeprecationReason = deprecationReason,
                Type = type,
                Resolver = new FuncFieldResolver<T>(resolve)
            };

            if (args != null)
            {
                var argConfig = new GraphQLArgumentConfig();
                args(argConfig);
                def.Arguments = argConfig.Arguments;
            }

            _fields.Add(def);
        }

        public void Field(
            string name,
            IGraphQLOutputType type,
            string description = null,
            string deprecationReason = null,
            Func<ResolveFieldContext, object> resolve = null,
            Action<GraphQLArgumentConfig> args = null)
        {
            Field<object>(name, type, description, deprecationReason, resolve, args);
        }
    }

    public class GraphQLTypeFieldConfig<T> : GraphQLTypeFieldConfig
    {
        public void Field<TProperty>(
            Expression<Func<T, TProperty>> resolve,
            bool nullable = false,
            string description = null,
            string deprecatedReason = null,
            Action<GraphQLArgumentConfig> args = null)
        {
            var name = resolve.NameOf();
            Field(name, resolve, nullable, description, deprecatedReason, args);
        }

        public void Field<TProperty>(
            string name,
            Expression<Func<T, TProperty>> resolve,
            bool nullable = false,
            string description = null,
            string deprecatedReason = null,
            Action<GraphQLArgumentConfig> args = null)
        {
            var def = new GraphQLFieldDefinition();
            def.Name = name.CamelCase();

            var type = typeof(TProperty);
            var outputType = (IGraphQLOutputType)type.GetGraphQLTypeFromType();

            if (!nullable)
            {
                outputType = new GraphQLNonNull(outputType);
            }

            def.Type = outputType;
            def.Resolver = new ExpressionFieldResolver<T, TProperty>(resolve);

            if (args != null)
            {
                var argConfig = new GraphQLArgumentConfig();
                args(argConfig);
                def.Arguments = argConfig.Arguments;
            }

            Field(def);
        }

        public void Field<TProperty>(
            string name,
            IGraphQLOutputType type,
            Expression<Func<T, TProperty>> resolve,
            string description = null,
            string deprecatedReason = null)
        {
            var def = new GraphQLFieldDefinition
            {
                Name = name,
                Type = type,
                Description = description,
                DeprecationReason = deprecatedReason,
                Resolver = new ExpressionFieldResolver<T, TProperty>(resolve)
            };

            Field(def);
        }

        public void Field<TResolve>(
            string name,
            IGraphQLOutputType type,
            string description = null,
            string deprecatedReason = null,
            Func<T, ResolveFieldContext, TResolve> resolve = null)
        {
            var def = new GraphQLFieldDefinition
            {
                Name = name,
                Type = type,
                Description = description,
                DeprecationReason = deprecatedReason,
                Resolver = new FuncFieldResolver<T, TResolve>(resolve)
            };

            Field(def);
        }
    }

    public interface IGraphQLObjectTypeConfig
    {
        string Name { get; set; }
        string Description { get; set; }
        string DeprecationReason { get; set; }
        Func<object, bool> IsOfType { get; set; }
        IEnumerable<GraphQLFieldDefinition> Fields { get; }
        IEnumerable<GraphQLInterfaceType> Interfaces { get; }
        void Interface(params GraphQLInterfaceType[] interfaces);
    }

    public class GraphQLObjectTypeConfigBase
    {
    }

    public class GraphQLObjectTypeConfig : GraphQLTypeFieldConfig, IGraphQLObjectTypeConfig
    {
        private readonly List<GraphQLInterfaceType> _interfaces = new List<GraphQLInterfaceType>();

        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public Func<object, bool> IsOfType { get; set; }

        public IEnumerable<GraphQLInterfaceType> Interfaces => _interfaces;

        public void Interface(params GraphQLInterfaceType[] interfaces)
        {
            _interfaces.AddRange(interfaces);
        }
    }

    public class GraphQLObjectTypeConfig<T> : GraphQLTypeFieldConfig<T>, IGraphQLObjectTypeConfig
    {
        private readonly List<GraphQLInterfaceType> _interfaces = new List<GraphQLInterfaceType>();

        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public Func<object, bool> IsOfType { get; set; }

        public IEnumerable<GraphQLInterfaceType> Interfaces => _interfaces;

        public void Interface(params GraphQLInterfaceType[] interfaces)
        {
            _interfaces.AddRange(interfaces);
        }
    }

    public class GraphQLArgument
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public object DefaultValue { get; set; }
        public IGraphQLInputType Type { get; set; }
    }

    public class GraphQLArgumentConfig
    {
        private readonly List<GraphQLArgument> _arguments = new List<GraphQLArgument>();

        public IEnumerable<GraphQLArgument> Arguments => _arguments;

        public void Argument(GraphQLArgument argument)
        {
            _arguments.Add(argument);
        }

        public void Argument(Action<GraphQLArgument> configure)
        {
            var arg = new GraphQLArgument();
            configure(arg);
            _arguments.Add(arg);
        }

        public void Argument<T>(
            string name,
            string description = null,
            string deprecationReason = null,
            T defaultValue = default(T),
            bool nullable = false)
        {
            var type = typeof(T);
            var inputType = (IGraphQLInputType)type.GetGraphQLTypeFromType();

            if (!nullable)
            {
                inputType = new GraphQLNonNull(inputType);
            }

            Argument(name, inputType, description, deprecationReason, defaultValue);
        }

        public void Argument(
            string name,
            IGraphQLInputType type,
            string description = null,
            string deprecationReason = null,
            object defaultValue = null)
        {
            var arg = new GraphQLArgument
            {
                Name = name,
                Type = type,
                Description = description,
                DeprecationReason = deprecationReason,
                DefaultValue = defaultValue,
            };
            _arguments.Add(arg);
        }
    }

    public class GraphQLFieldConfig : GraphQLArgumentConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DeprecationReason { get; set; }
        public IGraphQLOutputType Type { get; set; }
        public IFieldResolver Resolve { get; set; }
    }

    public interface IFieldResolver
    {
        object Resolve(ResolveFieldContext context);
    }

    public interface IFieldResolver<out T> : IFieldResolver
    {
        new T Resolve(ResolveFieldContext context);
    }

    public class FuncFieldResolver<K, T> : IFieldResolver<T>
    {
        private readonly Func<K, ResolveFieldContext, T> _resolver;

        public FuncFieldResolver(Func<K, ResolveFieldContext, T> resolver)
        {
            _resolver = resolver;
        }

        public T Resolve(ResolveFieldContext context)
        {
            return _resolver(context.SourceAs<K>(), context);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }

    public class FuncFieldResolver<T> : IFieldResolver<T>
    {
        private readonly Func<ResolveFieldContext, T> _resolver;

        public FuncFieldResolver(Func<ResolveFieldContext, T> resolver)
        {
            _resolver = resolver;
        }

        public T Resolve(ResolveFieldContext context)
        {
            return _resolver(context);
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }

    public class ExpressionFieldResolver<TObject, TProperty> : IFieldResolver<TProperty>
    {
        private readonly Expression<Func<TObject, TProperty>> _property;

        public ExpressionFieldResolver(Expression<Func<TObject, TProperty>> property)
        {
            _property = property;
        }

        public TProperty Resolve(ResolveFieldContext context)
        {
            return _property.Compile()(context.SourceAs<TObject>());
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }

    public class GraphQLObjectType : GraphQLType, IGraphQLOutputType
    {
        private readonly Dictionary<string, GraphQLFieldDefinition> _fields =
            new Dictionary<string, GraphQLFieldDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly List<GraphQLInterfaceType> _interfaces = new List<GraphQLInterfaceType>();

        public GraphQLObjectType()
        {
        }

        public GraphQLObjectType(IGraphQLObjectTypeConfig config)
        {
            Initialize(config);
        }

        public string Description { get; protected set; }
        public string DeprecationReason { get; protected set; }
        public Func<object, bool> IsOfType { get; protected set; }

        public void Initialize(IGraphQLObjectTypeConfig config)
        {
            Name = config.Name;
            Description = config.Description;
            DeprecationReason = config.DeprecationReason;
            Fields = config.Fields;
            Interfaces = config.Interfaces;
            IsOfType = config.IsOfType;

            Invariant.Check(
                !string.IsNullOrWhiteSpace(Name),
                "Type must be named.");

            if (Interfaces != null && Interfaces.Any())
            {
                Invariant.Check(
                    IsOfType != null,
                    $"{Name} does not provide a \"isTypeOf\" function.  There is no way to resolve this "
                    + "implementing type during execution.");
            }
        }

        public GraphQLFieldDefinition FieldFor(string name)
        {
            GraphQLFieldDefinition field;

            _fields.TryGetValue(name, out field);

            return field;
        }

        public IEnumerable<GraphQLFieldDefinition> Fields
        {
            get { return _fields.Values; }
            protected set
            {
                _fields.Clear();
                value.Apply(f => _fields[f.Name] = f);
            }
        }

        public IEnumerable<GraphQLInterfaceType> Interfaces
        {
            get { return _interfaces; }
            set
            {
                _interfaces.Clear();
                _interfaces.AddRange(value);
            }
        }

        public static GraphQLObjectType For(Action<GraphQLObjectTypeConfig> configure)
        {
            var config = new GraphQLObjectTypeConfig();
            configure(config);
            return new GraphQLObjectType(config);
        }
    }

    public class GraphQLObjectType<TModel> : GraphQLObjectType
    {
        public GraphQLObjectType()
        {
        }

        public GraphQLObjectType(GraphQLObjectTypeConfig<TModel> config)
            : base(config)
        {
        }

        public static GraphQLObjectType<TModel> For(Action<GraphQLObjectTypeConfig<TModel>> configure)
        {
            var type = typeof(TModel);

            var config = new GraphQLObjectTypeConfig<TModel>();
            var name = type.Name;

            if (type.IsInterface && name.StartsWith("I"))
            {
                name = name.Substring(1);
            }

            config.Name = name;

            configure(config);
            return new GraphQLObjectType<TModel>(config);
        }
    }

    public class GraphQLList : GraphQLType, IGraphQLInputType, IGraphQLOutputType
    {
        private IGraphQLType _ofType;

        public GraphQLList(IGraphQLType ofType)
        {
            SetValue(ofType);
        }

        public IGraphQLType OfType
        {
            get { return _ofType; }
            set
            {
                SetValue(value);
            }
        }

        private void SetValue(IGraphQLType type)
        {
            Invariant.Check(type != null, $"Can only create a list of GraphQLType but got: {type}");

            _ofType = type;
            Name = $"[{type}]";
        }
    }

    public class GraphQLNonNull : GraphQLType, IGraphQLInputType, IGraphQLOutputType
    {
        private IGraphQLType _ofType;

        public GraphQLNonNull(IGraphQLType ofType)
        {
            SetValue(ofType);
        }

        public IGraphQLType OfType
        {
            get { return _ofType; }
            set
            {
                SetValue(value);
            }
        }

        private void SetValue(IGraphQLType type)
        {
            Invariant.Check(
                type != null && type.GetType() != typeof(GraphQLNonNull),
                $"Can only create NonNull of a Nullable GraphQLType but got: {type}.");

            _ofType = type;
            Name = $"{type}!";
        }
    }

    public class GraphQLInterfaceType : GraphQLAbstractType
    {
        private readonly List<GraphQLFieldDefinition> _fields = new List<GraphQLFieldDefinition>();

        public GraphQLInterfaceType()
        {
        }

        public GraphQLInterfaceType(IGraphQLInterfaceTypeConfig config)
        {
            Initialize(config);
        }

        public string DeprecationReason { get; protected set; }

        public IEnumerable<GraphQLFieldDefinition> Fields
        {
            get { return _fields; }
            protected set
            {
                _fields.Clear();
                _fields.AddRange(value);
            }
        }

        public void Initialize(IGraphQLInterfaceTypeConfig config)
        {
            base.Initialize(config);
            Fields = config.Fields;
        }

        public static GraphQLInterfaceType For(Action<GraphQLInterfaceTypeConfig> configure)
        {
            var config = new GraphQLInterfaceTypeConfig();
            configure(config);
            return new GraphQLInterfaceType(config);
        }
    }

    public class GraphQLInterfaceType<TModel> : GraphQLInterfaceType
    {
        public GraphQLInterfaceType(GraphQLInterfaceTypeConfig<TModel> config)
            : base(config)
        {
        }

        public static GraphQLInterfaceType<TModel> For(Action<GraphQLInterfaceTypeConfig<TModel>> configure)
        {
            var type = typeof(TModel);

            var config = new GraphQLInterfaceTypeConfig<TModel>();
            var name = type.Name;

            if (type.IsInterface && name.StartsWith("I"))
            {
                name = name.Substring(1);
            }

            config.Name = name;

            configure(config);
            return new GraphQLInterfaceType<TModel>(config);
        }
    }

    /// <summary>
    /// A special type to allow an object/interface to reference itself.
    /// It is replaced with the real type object when the schema is built.
    /// </summary>
    public class GraphQLTypeReference : GraphQLType, IGraphQLOutputType
    {
        public GraphQLTypeReference(string typeName)
        {
            Name = $"ref({typeName})";
            TypeName = typeName;
        }

        public string TypeName { get; set; }
    }

    public class GraphQLDirective
    {
    }

    public class GraphQLAbstractType : GraphQLType, IGraphQLOutputType
    {
        private readonly List<IGraphQLType> _types = new List<IGraphQLType>();

        internal GraphQLAbstractType()
        {
        }

        public string Description { get; set; }

        public IEnumerable<IGraphQLType> Types => _types;

        protected void Initialize(IGraphQLAbstractTypeConfig config)
        {
            Name = config.Name;
            Description = config.Description;
        }

        public bool IsPossibleType(IGraphQLType type)
        {
            return Types.Any(x => x.Equals(type));
        }

        public void AddTypes(params IGraphQLType[] types)
        {
            _types.Fill(types);
        }
    }

    public class GraphQLUnionType : GraphQLAbstractType
    {
        public GraphQLUnionType()
        {
        }

        public GraphQLUnionType(GraphQLUnionTypeConfig config)
        {
            Initialize(config);
        }

        public void Initialize(GraphQLUnionTypeConfig config)
        {
            base.Initialize(config);
            AddTypes(config.Types);
        }

        public static GraphQLUnionType For(Action<GraphQLUnionTypeConfig> configure)
        {
            var config = new GraphQLUnionTypeConfig();
            configure(config);
            return new GraphQLUnionType(config);
        }
    }

    public interface IGraphQLAbstractTypeConfig
    {
        string Name { get; set; }
        string Description { get; set; }
        Func<object, GraphQLType> ResolveType { get; set; }
    }

    public interface IGraphQLInterfaceTypeConfig : IGraphQLAbstractTypeConfig
    {
        IEnumerable<GraphQLFieldDefinition> Fields { get; }
    }

    public class GraphQLInterfaceTypeConfig : GraphQLTypeFieldConfig, IGraphQLInterfaceTypeConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Func<object, GraphQLType> ResolveType { get; set; }
    }

    public class GraphQLInterfaceTypeConfig<TModel> : GraphQLTypeFieldConfig<TModel>, IGraphQLInterfaceTypeConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Func<object, GraphQLType> ResolveType { get; set; }
    }

    public class GraphQLUnionTypeConfig : IGraphQLAbstractTypeConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Func<object, GraphQLType> ResolveType { get; set; }
        public GraphQLObjectType[] Types { get; set; }
    }

    public class GraphQLSchemaWrapper
    {
        private readonly Dictionary<string, IGraphQLType> _types;
        private object _schema;

        public GraphQLSchemaWrapper(object schema)
        {
            _schema = schema;

            Query = GetProperty("query", schema);
            Mutation = GetProperty("mutation", schema);

            Invariant.Check(
                Query != null || Mutation != null,
                "A Query or Mutation property of type GraphQLObjectType is required.");

            var walker = new GraphQLTreeWalker();
            _types = walker.Walk(this);
        }

        public GraphQLObjectType Query { get; }
        public GraphQLObjectType Mutation { get; }

        public IGraphQLType TypeFor(string name)
        {
            var normalized = name.NormalizeTypeName();

            IGraphQLType type;

            _types.TryGetValue(normalized, out type);

            Invariant.Check(type != null, $"Unknown requested type '{normalized}'.");

            return type;
        }

        private GraphQLObjectType GetProperty(string name, object schema)
        {
            var type = schema.GetType();
            var info = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return info?.GetValue(schema, null) as GraphQLObjectType;
        }
    }

    public interface IReferenceReplacer
    {
        void Replace(object target, object type);
    }

    public interface IReferenceReplacer<T, K> : IReferenceReplacer
    {
        void Replace(T target, K type);
    }

    public class ReferenceReplacer<T, K> : IReferenceReplacer<T, K>
    {
        private readonly Action<T, K> _func;

        public ReferenceReplacer(Action<T, K> func)
        {
            _func = func;
        }

        public void Replace(T target, K type)
        {
            _func(target, type);
        }

        void IReferenceReplacer.Replace(object target, object type)
        {
            Replace((T) target, (K)type);
        }
    }

    public class ReferenceTarget
    {
        public GraphQLTypeReference Ref { get; set; }
        public object Target { get; set; }
        public IReferenceReplacer Replacer { get; set; }
    }

    public class GraphQLTreeWalker
    {
        private readonly List<ReferenceTarget> _references = new List<ReferenceTarget>();

        public Dictionary<string, IGraphQLType> Walk(GraphQLSchemaWrapper schema)
        {
            var types = new Dictionary<string, IGraphQLType>(StringComparer.OrdinalIgnoreCase);

            AddType(types, schema.Query);
            AddType(types, schema.Mutation);

            // replace any GraphQLReferenceTypes with real types
            _references.Apply(x =>
            {
                var refType = types[x.Ref.TypeName];
                x.Replacer.Replace(x.Target, refType);
            });

            return types;
        }

        private void AddType(Dictionary<string, IGraphQLType> types, IGraphQLType type)
        {
            if (type == null)
            {
                return;
            }

            if (type is GraphQLNonNull)
            {
                var nonNull = (GraphQLNonNull) type;
                if (nonNull.OfType is GraphQLTypeReference)
                {
                    var re = new ReferenceTarget
                    {
                        Ref = (GraphQLTypeReference) nonNull.OfType,
                        Target = nonNull,
                        Replacer =
                            new ReferenceReplacer<GraphQLNonNull, IGraphQLType>(
                                (target, repl) =>
                                {
                                    target.OfType = repl;
                                })
                    };

                    _references.Add(re);
                    return;
                }

                AddType(types, nonNull.OfType);
                return;
            }

            if (type is GraphQLList)
            {
                var list = (GraphQLList) type;
                if (list.OfType is GraphQLTypeReference)
                {
                    var re = new ReferenceTarget
                    {
                        Ref = (GraphQLTypeReference) list.OfType,
                        Target = list,
                        Replacer =
                            new ReferenceReplacer<GraphQLList, IGraphQLType>(
                                (target, repl) =>
                                {
                                    target.OfType = repl;
                                })
                    };

                    _references.Add(re);
                    return;
                }

                AddType(types, list.OfType);
                return;
            }

            if (type is GraphQLTypeReference)
            {
                return;
            }

            if (type.IsCoreType())
            {
                return;
            }

            if (types.ContainsKey(type.Name))
            {
                return;
            }

            types[type.Name] = type;

            if (type is GraphQLObjectType)
            {
                var objType = (GraphQLObjectType) type;
                objType.Fields.Apply(x =>
                {
                    x.Arguments?.Apply(a => AddType(types, a.Type));
                    AddType(types, x.Type);
                });

                objType.Interfaces.Apply(x =>
                {
                    x.AddTypes(objType);
                    AddType(types, x);
                });
            }

            if (type is GraphQLInterfaceType)
            {
                var inter = (GraphQLInterfaceType) type;
                inter.Fields.Apply(x =>
                {
                    x.Arguments?.Apply(a => AddType(types, a.Type));
                    if (x.Type is GraphQLTypeReference)
                    {
                        var re = new ReferenceTarget
                        {
                            Ref = (GraphQLTypeReference) x.Type,
                            Target = x,
                            Replacer =
                                new ReferenceReplacer<GraphQLFieldDefinition, IGraphQLOutputType>(
                                    (target, repl) =>
                                    {
                                        target.Type = repl;
                                    })
                        };

                        _references.Add(re);
                    }
                    else
                    {
                        AddType(types, x.Type);
                    }
                });
            }

            if (type is GraphQLUnionType)
            {
                var union = (GraphQLUnionType) type;
                union.Types.Apply(x =>
                {
                    AddType(types, x);
                });
            }
        }
    }

    public class GraphQLExecutionContext
    {
        public GraphQLExecutionContext()
        {
            Fragments = new Fragments();
            Errors = new ExecutionErrors();
        }

        public GraphQLSchemaWrapper Schema { get; set; }

        public object RootValue { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; }

        public Variables Variables { get; set; }

        public ExecutionErrors Errors { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }

    public class GraphQLEngine
    {
        private readonly GraphQLEngineConfig _config;

        public GraphQLEngine(GraphQLEngineConfig config)
        {
            _config = config;
        }

        public async Task<ExecutionResult> ExecuteAsync(
            string query,
            string operationName = null,
            object root = null,
            Inputs inputs = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new ExecutionResult();
            var document = _config.DocumentBuilder.Build(query);

            var context = BuildExecutionContext(_config.Schema, root, document, operationName, inputs, cancellationToken);

            if (context.Errors.Any())
            {
                result.Errors = context.Errors;
                return result;
            }

//            result.Data = await ExecuteOperation(context);
            if (context.Errors.Any())
            {
                result.Errors = context.Errors;
            }

            return result;
        }

        public static GraphQLEngine For(object schema)
        {
            var engineConfig = new GraphQLEngineConfig();
            engineConfig.DocumentBuilder = new AntlrDocumentBuilder();
            engineConfig.Schema = new GraphQLSchemaWrapper(schema);
            return new GraphQLEngine(engineConfig);
        }

        public GraphQLExecutionContext BuildExecutionContext(
            GraphQLSchemaWrapper schema,
            object root,
            Document document,
            string operationName,
            Inputs inputs,
            CancellationToken cancellationToken)
        {
            var context = new GraphQLExecutionContext();
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
//            context.Variables = GetVariableValues(schema, operation.Variables, inputs);
            context.Fragments = document.Fragments;
            context.CancellationToken = cancellationToken;

            return context;
        }
    }

    public class GraphQLEngineConfig
    {
        private readonly List<GraphQLType> _additionalTypes = new List<GraphQLType>();
        private readonly List<GraphQLDirective> _directives = new List<GraphQLDirective>();

        public GraphQLSchemaWrapper Schema { get; set; }
        public IDocumentBuilder DocumentBuilder { get; set; }
        public IEnumerable<GraphQLType> AdditionalTypes => _additionalTypes;
        public IEnumerable<GraphQLDirective> Directives => _directives;

        public void RegisterTypes(params GraphQLType[] types)
        {
            Invariant.Check(types == null, $"{types} cannot be null.");

            types.Apply(RegisterType);
        }

        public void RegisterType<T>() where T : GraphQLType, new()
        {
            RegisterType(new T());
        }

        private void RegisterType(GraphQLType type)
        {
            _additionalTypes.Fill(type);
        }
    }

    public class GraphQLError : Exception
    {
        public GraphQLError(string message)
            : base(message)
        {
        }
    }

    public class Invariant
    {
        public static void Check(bool valid, string message)
        {
            if (!valid)
            {
                throw new GraphQLError(message);
            }
        }
    }

    public static class GraphQLExtensions
    {
        public static string NormalizeTypeName(this string s)
        {
            return s.Trim('!').TrimStart('[').TrimEnd(']');
        }

        public static string CamelCase(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            return $"{char.ToLowerInvariant(s[0])}{s.Substring(1)}";
        }

        public static string PascalCase(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            return $"{char.ToUpperInvariant(s[0])}{s.Substring(1)}";
        }

        public static IGraphQLType GetGraphQLTypeFromType(this Type type)
        {
            if (type == typeof(int))
            {
                return GraphQLScalarTypes.GraphQLInt;
            }

            if (type == typeof(long))
            {
                return GraphQLScalarTypes.GraphQLInt;
            }

            if (type == typeof(double))
            {
                return GraphQLScalarTypes.GraphQLFloat;
            }

            if (type == typeof(string))
            {
                return GraphQLScalarTypes.GraphQLString;
            }

            if (type == typeof(bool))
            {
                return GraphQLScalarTypes.GraphQLBoolean;
            }

            throw new ArgumentOutOfRangeException(nameof(type), "Unknown input type.");
        }

        public static bool IsCoreType(this IGraphQLType type)
        {
            IGraphQLType[] scalars = {
                GraphQLScalarTypes.GraphQLID,
                GraphQLScalarTypes.GraphQLBoolean,
                GraphQLScalarTypes.GraphQLFloat,
                GraphQLScalarTypes.GraphQLInt,
                GraphQLScalarTypes.GraphQLString
            };

            IGraphQLType[] introspection = {
            };

            return scalars.Any(s => s.Equals(type))
                   || introspection.Any(i => i.Equals(type));
        }

        public static string NameOf<T,P>(this Expression<Func<T,P>> expression)
        {
            var member = (MemberExpression)expression.Body;
            return member.Member.Name;
        }
    }
}
