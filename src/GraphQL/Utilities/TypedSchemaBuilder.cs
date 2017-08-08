using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public class TypedSchemaBuilder
    {
        private readonly IDictionary<string, IGraphType> _graphTypes = new Dictionary<string, IGraphType>();

        public IDependencyResolver DependencyResolver { get; set; } = new DefaultDependencyResolver();
        public TypeToGraphTypeConverter TypeConverter { get; set; } = new TypeToGraphTypeConverter();

        public ISchema Build(IEnumerable<Type> types)
        {
            var schema = new Schema();

            types.Apply(type =>
            {
                var graphType = BuildObjectType(type);
                _graphTypes[graphType.Name] = graphType;
                TypeConverter.Register(type, graphType);
            });

            schema.Query = GetType("Query") as IObjectGraphType;
            schema.Mutation = GetType("Mutation") as IObjectGraphType;
            schema.Subscription = GetType("Subscription") as IObjectGraphType;

            schema.RegisterTypes(_graphTypes.Values.ToArray());

            return schema;
        }

        private IGraphType GetType(string name)
        {
            _graphTypes.TryGetValue(name, out IGraphType type);
            return type;
        }

        private IObjectGraphType BuildObjectType(Type type)
        {
            var attr = type.GetTypeInfo().GetCustomAttribute<GraphQLMetadataAttribute>();

            var objectType = new ObjectGraphType();
            objectType.Name = attr?.Name ?? type.Name;
            objectType.Description = attr?.Description;
            objectType.DeprecationReason = attr?.DeprecationReason;
            objectType.IsTypeOf = obj => obj?.GetType().IsAssignableFrom(type) ?? false;

            var fields = type.Methods().Select(m => BuildField(type, m)).Where(x => x != null);
            fields.Apply(f => objectType.AddField(f));

            fields = type.Properties().Select(p => BuildField(type, p)).Where(x => x != null);
            fields.Apply(f => objectType.AddField(f));

            return objectType;
        }

        private IGraphType GetTypeReference(Type type)
        {
            var targetType = type;

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                targetType = targetType.GenericTypeArguments[0];
            }

            if (targetType.GetTypeInfo().IsGenericType &&
                targetType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var listType = GetTypeReference(targetType.GenericTypeArguments[0]);
                return new ListGraphType(listType);
            }

            var objType = TypeConverter.Convert(targetType);

            if (objType != null) return objType;

            objType = BuildObjectType(targetType);
            if (objType != null)
            {
                TypeConverter.Register(targetType, objType);
            }
            return objType;
        }

        private FieldType BuildField(Type parentType, PropertyInfo property)
        {
            var attr = property.GetCustomAttribute<GraphQLMetadataAttribute>();

            var field = new FieldType();
            field.Name = attr?.Name ?? property.Name.ToCamelCase();
            field.Description = attr?.Description;
            field.DeprecationReason = attr?.DeprecationReason;
            field.ResolvedType = GetTypeReference(property.PropertyType);
            return field;
        }

        private FieldType BuildField(Type parentType, MethodInfo method)
        {
            if (method.ReturnType == typeof(void))
            {
                return null;
            }

            var attr = method.GetCustomAttribute<GraphQLMetadataAttribute>();

            var field = new FieldType();
            field.Name = attr?.Name ?? method.Name.ToCamelCase();
            field.Description = attr?.Description;
            field.DeprecationReason = attr?.DeprecationReason;
            field.Arguments = GetArguments(method);
            field.ResolvedType = GetTypeReference(method.ReturnType);

            var resolverType = typeof(MethodModelBinderResolver<>).MakeGenericType(parentType);

            var args = new object[] { method, DependencyResolver };
            var resolver = (IFieldResolver)Activator.CreateInstance(resolverType, args);
            field.Resolver = resolver;

            return field;
        }

        private QueryArguments GetArguments(MethodInfo method)
        {
            var parameters = method.GetParameters().Select(GetArgument).Where(x => x != null);
            var args = new QueryArguments(parameters);
            return args;
        }

        private QueryArgument GetArgument(ParameterInfo parameter)
        {
            var graphType = TypeConverter.Convert(parameter.ParameterType);

            if (graphType == null)
            {
                return null;
            }

            var arg = new QueryArgument(graphType);
            arg.Name = parameter.Name.ToCamelCase();
            return arg;
        }
    }

    public class TypeToGraphTypeConverter
    {
        private readonly IDictionary<Type, IGraphType> _typeMap;

        public TypeToGraphTypeConverter()
        {
            var stringType = new GraphQLTypeReference("String");
            var booleanType = new GraphQLTypeReference("Boolean");
            var integerType = new GraphQLTypeReference("Int");
            var floatType = new GraphQLTypeReference("Float");
            var decimalType = new GraphQLTypeReference("Decimal");
            var dateType = new GraphQLTypeReference("Date");
            var idType = new GraphQLTypeReference("ID");

            _typeMap = new Dictionary<Type, IGraphType>
            {
                { typeof(void), booleanType },
                { typeof(string), stringType},
                { typeof(bool), booleanType},
                { typeof(byte), integerType},
                { typeof(char), integerType},
                { typeof(short), integerType},
                { typeof(ushort), integerType },
                { typeof(int), integerType },
                { typeof(uint), integerType },
                { typeof(long), integerType },
                { typeof(ulong), integerType },
                { typeof(float), floatType },
                { typeof(double), floatType },
                { typeof(decimal), decimalType },
                { typeof(DateTime), dateType },
                { typeof(Guid), idType },
            };
        }

        public IGraphType Convert(Type type)
        {
            _typeMap.TryGetValue(type, out IGraphType graphType);
            return graphType;
        }

        public void Register(Type type, IGraphType graphType)
        {
            _typeMap[type] = graphType;
        }
    }

    public static class TypeExtensions
    {
        public static IEnumerable<PropertyInfo> Properties(this Type type)
        {
            return type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(x => !x.IsSpecialName);
        }

        public static IEnumerable<MethodInfo> Methods(this Type type)
        {
            return type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(x => !x.IsSpecialName);
        }
    }
}
