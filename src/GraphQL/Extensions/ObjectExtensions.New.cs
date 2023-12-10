using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for objects and a method for converting a dictionary into a strongly typed object.
    /// </summary>
    public static partial class ObjectExtensions
    {
        /// <summary>
        /// Reads fields
        /// </summary>
        public static object? ToObjectNew(IDictionary<string, object?> map, Type sourceType, IInputObjectGraphType graphType)
        {
            var fn = CreateToObjectNewFunction(sourceType, graphType);
            return fn(map);
        }

        /// <summary>
        /// Gets reflection information based on the specified graph type and CLR type.
        /// </summary>
        internal static (ConstructorInfo Constructor, List<(string Key, Type ClrType, IGraphType GraphType)> CtorFields, List<(string Key, MemberInfo Member, Type Type, IGraphType ResolvedType)> MemberFields) GetReflectionInformation(Type clrType, IInputObjectGraphType graphType)
        {
            // gather for each field: dictionary key, clr property name, and graph type
            var fields = new List<(string Key, string? MemberName, IGraphType ResolvedType)>(graphType.Fields.Count);
            foreach (var field in graphType.Fields)
            {
                // get clr property name (also used for matching on field name or constructor parameter name)
                var fieldName = field.GetMetadata<string>(InputObjectGraphType.ORIGINAL_EXPRESSION_PROPERTY_NAME) ?? field.Name;
                // get graph type
                var resolvedType = field.ResolvedType
                    ?? throw new InvalidOperationException($"Field '{field.Name}' of graph type '{graphType.Name}' does not have the ResolvedType property set.");
                // add to list
                fields.Add((field.Name, fieldName, resolvedType));
            }
            // validate that no two different fields use the same member
            var memberNames = new HashSet<string>(fields.Select(x => x.MemberName!));
            if (memberNames.Count != fields.Count)
                throw new InvalidOperationException($"Two fields within graph type '{graphType.Name}' were mapped to the same member.");
            // find best constructor to use, with preference to the constructor with the most parameters
            var bestConstructor = AutoRegisteringHelper.GetConstructor(clrType);
            // pull out parameters that are applicable for that constructor
            var ctorParameters = bestConstructor.GetParameters();
            var ctorFields = new List<(string Key, Type ClrType, IGraphType GraphType)>();
            foreach (var ctorParam in ctorParameters)
            {
                // look for a field that matches the constructor parameter name
                var index = fields.FindIndex(x => string.Equals(x.MemberName, ctorParam.Name, StringComparison.OrdinalIgnoreCase));
                if (index == -1)
                    throw new InvalidOperationException($"Cannot find field named '{ctorParam.Name}' on graph type '{graphType.Name}' to fulfill constructor parameter for type '{clrType.GetFriendlyName()}'.");
                // add to list, and mark to be removed from fields
                var value = fields[index];
                ctorFields.Add((value.Key, ctorParam.ParameterType, value.ResolvedType));
                value.MemberName = null;
                fields[index] = value;
            }
            // remove fields that were used in the constructor
            fields.RemoveAll(x => x.MemberName == null);
            // find other members
            var objProperties = clrType.GetProperties().Where(x => x.SetMethod?.IsPublic ?? false).ToList();
            var objFields = clrType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            var members = new List<(string Key, MemberInfo Member, Type Type, IGraphType ResolvedType)>(fields.Count);
            foreach (var field in fields)
            {
                // check properties
                var objProp = objProperties.SingleOrDefault(x => string.Equals(x.Name, field.MemberName, StringComparison.OrdinalIgnoreCase));
                if (objProp != null)
                {
                    members.Add((field.Key, objProp, objProp.PropertyType, field.ResolvedType));
                    continue;
                }
                var objField = objFields.SingleOrDefault(x => string.Equals(x.Name, field.MemberName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Cannot find member named '{field.MemberName}' on CLR type '{clrType.GetFriendlyName()}'.");
                members.Add((field.Key, objField, objField.FieldType, field.ResolvedType));
            }

            return (bestConstructor, ctorFields, members);
        }

        /// <summary>
        /// Compiles a function to convert a dictionary to an object based on a specified <see cref="IInputObjectGraphType"/> instance.
        /// </summary>
        public static Func<IDictionary<string, object?>, object> CreateToObjectNewFunction(Type sourceType, IInputObjectGraphType graphType)
        {
            var (bestConstructor, ctorFields, members) = GetReflectionInformation(sourceType, graphType);

            // create expression; start with the parameter
            // then build the members
            var initExpression = Expression.MemberInit(
                Expression.New(bestConstructor, ctorFields.Select(f => GetExpressionForParameter(f.Key, f.ClrType, f.GraphType))),
                members.Select(member => Expression.Bind(member.Member, GetExpressionForParameter(member.Key, member.Type, member.ResolvedType))));

            // build the lambda
            var lambda = Expression.Lambda<Func<IDictionary<string, object?>, object>>(Expression.Convert(initExpression, typeof(object)), _dictionaryParam);
            return lambda.Compile();

            static Expression GetExpressionForParameter(string key, Type type, IGraphType graphType)
            {
                var expr = Expression.Call(_getOrDefaultMethod, _dictionaryParam, Expression.Constant(key, typeof(string)));
                return CoerceExpression(expr, type, graphType);
            }

            static Expression CoerceExpression(Expression expr, Type type, IGraphType graphType)
            {
                // unwrap non-null graph type
                graphType = graphType is NonNullGraphType nonNullGraphType
                    ? nonNullGraphType.ResolvedType ?? throw new InvalidOperationException($"ResolvedType is null for graph type '{nonNullGraphType}'")
                    : graphType;

                if (graphType is ListGraphType listGraphType)
                {
                    var elementGraphType = listGraphType.ResolvedType ?? throw new InvalidOperationException();
                    // determine the type of list to create
                    var isArray = false;
                    Type? elementType = null;
                    if (type.IsArray)
                    {
                        elementType = type.GetElementType()!;
                        isArray = true;
                    }
                    else if (type == typeof(IEnumerable) || type == typeof(object))
                    {
                        elementType = typeof(object);
                    }
                    else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        elementType = type.GetGenericArguments()[0];
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        elementType = type.GetInterfaces()
                            .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                            ?.GetGenericArguments()[0];
                    }
                    if (elementType == null)
                        throw new InvalidOperationException($"Could not determine enumerable type for type '{type.GetFriendlyName()}' while coercing graph type '{graphType}'.");
                    // create an expression that represents this:
                    // (IEnumerable<object>?)expr?.Select(x => CoerceExpression(x, elementType, listGraphType.ResolvedType))
                    var loopVar = Expression.Parameter(typeof(object));
                    var loopContent = CoerceExpression(loopVar, elementType, elementGraphType);
                    var expr2 = Select(Expression.Convert(expr, typeof(IEnumerable)), loopVar, loopContent, isArray);
                    return Expression.Convert(expr2, type);
                }
                return Expression.Call(_getPropertyValueTypedMethod.MakeGenericMethod(type), expr, Expression.Constant(type), Expression.Constant(graphType));
            }
        }

        private static readonly MethodInfo _getPropertyValueTypedMethod = typeof(ObjectExtensions).GetMethod(nameof(GetPropertyValueTyped), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static T? GetPropertyValueTyped<T>(object? propertyValue, Type fieldType, IGraphType mappedType)
        {
            // if the property is null return the default value
            if (propertyValue == null)
                return default;

            // Short-circuit conversion if the property value already of the right type
            if (fieldType == typeof(object) || fieldType.IsInstanceOfType(propertyValue))
                return (T?)propertyValue;

            if (ValueConverter.TryConvertTo(propertyValue, fieldType, out object? result))
                return (T?)result;


            var ret = GetPropertyValue(propertyValue, fieldType, mappedType);
            if (ret == null)
                return default;
            return (T)ret;
        }

        private static Expression Select(Expression collection, ParameterExpression loopVar, Expression loopContent, bool asArray)
        {
            var collectionVar = Expression.Variable(collection.Type);
            var enumeratorVar = Expression.Variable(typeof(IEnumerator));
            var getEnumeratorCall = Expression.Call(collectionVar, typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator))!);
            var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!);
            var getCurrent = Expression.MakeMemberAccess(enumeratorVar, typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current))!);

            var breakLabel = Expression.Label("label1");
            var returnLabel = Expression.Label("label2");

            var listVariable = Expression.Variable(typeof(List<>).MakeGenericType(loopContent.Type));
            var returnVariable = Expression.Variable(asArray ? loopContent.Type.MakeArrayType() : listVariable.Type);
            var addMethod = listVariable.Type.GetMethod(nameof(List<object>.Add))!;
            var toArrayMethod = listVariable.Type.GetMethod(nameof(List<object>.ToArray))!;

            var loop = Expression.Block(
                returnVariable.Type,
                new[] { collectionVar, enumeratorVar, listVariable, returnVariable },
                Expression.Assign(collectionVar, collection),
                Expression.Assign(returnVariable, Expression.Constant(null, returnVariable.Type)),
                Expression.IfThen(
                    Expression.Equal(collectionVar, Expression.Constant(null, collection.Type)),
                    Expression.Goto(returnLabel)),
                Expression.Assign(enumeratorVar, getEnumeratorCall),
                Expression.Assign(listVariable, Expression.New(listVariable.Type)),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.Equal(moveNextCall, Expression.Constant(true)),
                        Expression.Block(new[] { loopVar },
                            Expression.Assign(loopVar, getCurrent),
                            Expression.Call(listVariable, addMethod, loopContent)),
                        Expression.Break(breakLabel)),
                    breakLabel),
                Expression.Assign(returnVariable, asArray ? Expression.Call(listVariable, toArrayMethod) : listVariable),
                Expression.Label(returnLabel),
                returnVariable);

            return loop;
        }

        private static readonly ParameterExpression _dictionaryParam = Expression.Parameter(typeof(IDictionary<string, object?>));
        private static readonly MethodInfo _getOrDefaultMethod = typeof(ObjectExtensions).GetMethod(nameof(GetOrDefaultImplementation), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static object? GetOrDefaultImplementation(IDictionary<string, object?> obj, string key)
            => obj.TryGetValue(key, out var value) ? value : default;
    }
}
