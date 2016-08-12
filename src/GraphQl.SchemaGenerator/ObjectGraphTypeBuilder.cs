using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.SchemaGenerator.Attributes;
using GraphQL.SchemaGenerator.Extensions;
using GraphQL.SchemaGenerator.Helpers;
using GraphQL.Types;

namespace GraphQL.SchemaGenerator
{
    public static class ObjectGraphTypeBuilder
    {
        public static void Build(ObjectGraphType graphType, Type type)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                throw new InvalidOperationException("type must not be an abstract type or an interface");
            }

            ProcessObjectType(graphType, type);

            bool hasDataContract = type.ShouldIncludeInGraph();

            // KnownTypeAttribute could be used when SchemaType and DomainType are the same
            ProcessType(graphType, type);
            ProcessProperties(graphType, GetProperties(hasDataContract, type));
            ProcessFields(graphType, GetFields(hasDataContract, type));
            ProcessMethods(graphType, type, type.GetMethods());
        }

        public static void Build(InterfaceGraphType graphType, Type type)
        {
            if (!type.IsInterface && !type.IsAbstract)
            {
                throw new InvalidOperationException("type must be an abstract type or an interface");
            }

            ProcessInterfaceType(graphType, type);

            bool hasDataContract = type.ShouldIncludeInGraph();

            // KnownTypeAttribute could be used when SchemaType and DomainType are the same
            ProcessType(graphType, type);
            ProcessProperties(graphType, GetProperties(hasDataContract, type));
            ProcessFields(graphType, GetFields(hasDataContract, type));
            ProcessMethods(graphType, type, type.GetMethods());
        }

        public static void Build(InputObjectGraphType graphType, Type type)
        {
            ProcessType(graphType, type);
            bool hasDataContract = type.ShouldIncludeInGraph();
            ProcessProperties(graphType, GetProperties(hasDataContract, type));
            ProcessFields(graphType, GetFields(hasDataContract, type));
            ProcessMethods(graphType, type, type.GetMethods());
        }

        private static IEnumerable<PropertyInfo> GetProperties(bool hasDataContract, Type type)
        {
            var properties = type.GetProperties();
            if (hasDataContract)
            {
                return properties.Where(p => p.ShouldIncludeMemberInGraph());
            }
            else
            {
                return properties;
            }
        }

        private static IEnumerable<FieldInfo> GetFields(bool hasDataContract, Type type)
        {
            var fields = type.GetFields();
            if (hasDataContract)
            {
                return fields.Where(f => f.ShouldIncludeMemberInGraph());
            }
            else
            {
                return fields;
            }
        }

        private static void ProcessInterfaceType(InterfaceGraphType interfaceGraphType, Type type)
        {
            interfaceGraphType.ResolveType = CreateResolveType(type);
        }

        private static void ProcessObjectType(ObjectGraphType objectGraphType, Type type)
        {
            var interfaces = new List<Type>();
            foreach (var @interface in type.GetInterfaces())
            {
                if (!IsGraphType(@interface))
                {
                    continue;
                }
                interfaces.Add(GraphTypeConverter.ConvertTypeToGraphType(type));
            }

            objectGraphType.Interfaces = interfaces;
        }

        private static bool IsGraphType(Type @interface)
        {
            return TypeHelper.GetGraphType(@interface) != null ||
                @interface.ShouldIncludeInGraph();
        }

        private static void ProcessType(GraphType graphType, Type type)
        {
            graphType.Name = TypeHelper.GetDisplayName(type);
            
            var descAttr = type.GetCustomAttribute<DescriptionAttribute>();
            if (descAttr != null)
            {
                graphType.Description = descAttr.Description;
            }
            // explicit - include with DataMember, implicit - exclude with GraphIgnore            
        }

        private static Func<object, ObjectGraphType> CreateResolveType(Type type)
        {
            var expressions = new List<Expression>();
            var knownTypes = TypeHelper.GetGraphKnownTypes(type);

            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var returnLabel = Expression.Label(typeof(ObjectGraphType));

            foreach (var knownType in knownTypes)
            {
                var graphType = GraphTypeConverter.ConvertTypeToGraphType(knownType.SchemaType);
                var lookup = Expression.IfThen(
                    Expression.TypeIs(instanceParam, knownType.DomainType),
                    Expression.Return(returnLabel, Expression.Convert(Expression.New(graphType), typeof(ObjectGraphType)))
                );

                expressions.Add(lookup);
            }

            var result = Expression.Convert(Expression.Constant(null), typeof(ObjectGraphType));
            expressions.Add(Expression.Label(returnLabel, result));
            var body = Expression.Block(expressions);

            return Expression.Lambda<Func<object, ObjectGraphType>>(
                body,
                instanceParam).Compile();
        }

        private static void ProcessProperties(GraphType graphType, IEnumerable<PropertyInfo> properties)
        {
            foreach (var property in properties.OrderBy(p => p.Name))
            {
                bool isNotNull = TypeHelper.IsNotNull(property);

                var propertyGraphType = TypeHelper.GetGraphType(property);
                if (propertyGraphType != null)
                {
                    propertyGraphType = GraphTypeConverter.ConvertTypeToGraphType(propertyGraphType, isNotNull);
                    propertyGraphType = EnsureList(property.PropertyType, propertyGraphType);
                }
                else
                {
                    propertyGraphType = GraphTypeConverter.ConvertTypeToGraphType(property.PropertyType, isNotNull);
                }

                var name = StringHelper.GraphName(property.Name);
                var field = graphType.Field(
                    propertyGraphType,
                    name,
                    TypeHelper.GetDescription(property));

                field.DefaultValue = TypeHelper.GetDefaultValue(property);
                field.DeprecationReason = TypeHelper.GetDeprecationReason(property);
            }
        }

        private static void ProcessFields(GraphType graphType, IEnumerable<FieldInfo> fields)
        {
            foreach (var field in fields.OrderBy(f => f.Name))
            {
                bool isNotNull = TypeHelper.IsNotNull(field);

                var fieldGraphType = TypeHelper.GetGraphType(field);
                if (fieldGraphType != null)
                {
                    fieldGraphType = GraphTypeConverter.ConvertTypeToGraphType(fieldGraphType, isNotNull);
                    fieldGraphType = EnsureList(field.FieldType, fieldGraphType);
                }
                else
                {
                    fieldGraphType = GraphTypeConverter.ConvertTypeToGraphType(field.FieldType, isNotNull);
                }

                graphType.Field(                
                    fieldGraphType,
                    StringHelper.GraphName(field.Name));
            }
        }

        private static void ProcessMethods(GraphType graphType, Type type, IEnumerable<MethodInfo> methods)
        {
            if (!typeof(GraphType).IsAssignableFrom(type) &&
                !type.IsDefined(typeof(GraphTypeAttribute)))
            {
                return;
            }
            foreach (var method in methods.OrderBy(m => m.Name))
            {
                if (IsSpecialMethod(method))
                {
                    continue;
                }

                bool isNotNull = TypeHelper.IsNotNull(method);
                var returnGraphType = TypeHelper.GetGraphType(method);
                var methodGraphType = returnGraphType;
                if (methodGraphType != null)
                {
                    methodGraphType = GraphTypeConverter.ConvertTypeToGraphType(methodGraphType, isNotNull);
                    methodGraphType = EnsureList(method.ReturnType, methodGraphType);
                }
                else
                {
                    methodGraphType = GraphTypeConverter.ConvertTypeToGraphType(method.ReturnType, isNotNull);
                }

                graphType.Field(
                    methodGraphType,
                    StringHelper.GraphName(method.Name),
                    null,
                    new QueryArguments(method.GetParameters().Where(p => p.ParameterType != typeof(ResolveFieldContext)).Select(p => CreateArgument(p))),
                    // todo: need to fix method execution - not called currently so lower priority
                    c => method.Invoke(Activator.CreateInstance(type), GetArguments(method, c))
                );
            }
        }

        private static Type EnsureList(Type type, Type methodGraphType)
        {
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                methodGraphType = typeof(ListGraphType<>).MakeGenericType(methodGraphType);
            }

            return methodGraphType;
        }

        private static object[] GetArguments(MethodInfo method, ResolveFieldContext context)
        {
            var list = new List<object>();

            foreach (var param in method.GetParameters())
            {
                if (param.ParameterType == typeof(ResolveFieldContext))
                {
                    list.Add(context);
                    continue;
                }

                object value;
                if (context.Arguments.TryGetValue(param.Name, out value))
                {
                    list.Add(value);
                }
            }

            return list.ToArray();
        }

        public static bool IsSpecialMethod(MethodInfo method)
        {
            return method.IsSpecialName || method.DeclaringType == typeof(object);
        }

        private static QueryArgument CreateArgument(ParameterInfo parameter)
        {
            var isNotNull = TypeHelper.IsNotNull(parameter);
            var parameterGraphType = TypeHelper.GetGraphType(parameter);
            if (parameterGraphType != null)
            {
                parameterGraphType = GraphTypeConverter.ConvertTypeToGraphType(parameterGraphType, isNotNull);
                parameterGraphType = EnsureList(parameter.ParameterType, parameterGraphType);
            }
            else
            {
                parameterGraphType = GraphTypeConverter.ConvertTypeToGraphType(parameter.ParameterType, isNotNull);
            }

            return new QueryArgument(parameterGraphType)
            {
                Name = parameter.Name,
                DefaultValue = TypeHelper.GetDefaultValue(parameter),
                Description = TypeHelper.GetDescription(parameter),
            };
        }
    }
}
