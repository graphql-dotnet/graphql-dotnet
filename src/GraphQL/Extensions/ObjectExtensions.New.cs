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
        /// Compiles a function to convert a dictionary to an object based on a specified <see cref="IInputObjectGraphType"/> instance.
        /// </summary>
        public static Func<IDictionary<string, object?>, object> CreateToObjectNewFunction(Type sourceType, IInputObjectGraphType graphType)
        {
            var info = GetReflectionInformation(sourceType, graphType);
            var bestConstructor = info.Constructor;
            var ctorFields = info.CtorFields;
            var members = info.MemberFields;

            // create expression; start with the parameter
            // then build the members
            var initExpression = Expression.MemberInit(
                Expression.New(bestConstructor, ctorFields.Select(GetExpressionForParameter)),
                members.Where(x => x.IsRequired || x.IsInitOnly).Select(member =>
                {
                    var type = member.Member is PropertyInfo propertyInfo ? propertyInfo.PropertyType : ((FieldInfo)member.Member).FieldType;
                    return Expression.Bind(member.Member, GetExpressionForParameter2(member.Key, type, member.GraphType));
                }));

            // build the lambda
            var lambda = Expression.Lambda<Func<IDictionary<string, object?>, object>>(Expression.Convert(initExpression, typeof(object)), _dictionaryParam);
            return lambda.Compile();

            static Expression GetExpressionForParameter((string? Key, ParameterInfo ParameterInfo, IGraphType? ResolvedType) member)
            {
                return member.Key != null
                    ? GetExpressionForParameter2(member.Key, member.ParameterInfo.ParameterType, member.ResolvedType!)
                    : Expression.Constant(member.ParameterInfo.DefaultValue, member.ParameterInfo.ParameterType);
            }

            static Expression GetExpressionForParameter2(string key, Type type, IGraphType graphType)
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
