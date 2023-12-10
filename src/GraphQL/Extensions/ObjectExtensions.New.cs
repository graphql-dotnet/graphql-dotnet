using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL;

public static partial class ObjectExtensions
{
    /// <summary>
    /// Compiles a function to convert a dictionary to an object based on a specified <see cref="IInputObjectGraphType"/> instance.
    /// </summary>
    public static Func<IDictionary<string, object?>, object> CompileToObject(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type sourceType,
        IInputObjectGraphType graphType)
    {
        var info = GetReflectionInformation(sourceType, graphType);
        var bestConstructor = info.Constructor;
        var ctorFields = info.CtorFields;
        var members = info.MemberFields;

        // T obj;
        var objParam = Expression.Variable(sourceType, "obj");

        // obj = new T(...) { ... };
        var expressions = new List<Expression>(members.Count(x => !x.IsRequired && !x.IsInitOnly) + 2)
        {
            Expression.Assign(
            objParam,
            Expression.MemberInit(
                Expression.New(bestConstructor, ctorFields.Select(GetExpressionForCtorParameter)),
                members.Where(x => x.IsRequired || x.IsInitOnly).Select(GetBindingForMember)))
        };

        // set properties on obj when they exist in the dictionary
        foreach (var member in members.Where(x => !x.IsRequired && !x.IsInitOnly))
        {
            expressions.Add(ConditionallySetMember(objParam, member));
        }

        // return obj;
        expressions.Add(objParam);

        var block = Expression.Block(new[] { objParam }, expressions);

        // build the lambda
        var lambda = Expression.Lambda<Func<IDictionary<string, object?>, object>>(
            Expression.Convert(block, typeof(object)),
            _dictionaryParam);

        // compile the lambda and return it
        return lambda.Compile();

        static Expression GetExpressionForCtorParameter(ReflectionInfo.CtorParameterInfo member)
        {
            if (member.Key == null)
                return Expression.Constant(member.ParameterInfo.DefaultValue, member.ParameterInfo.ParameterType);

            return GetCoerceOrDefault(
                member.Key,
                member.ParameterInfo.ParameterType,
                member.GraphType!);
        }

        static MemberAssignment GetBindingForMember(ReflectionInfo.MemberFieldInfo member)
        {
            var type = member.Member is PropertyInfo propertyInfo ? propertyInfo.PropertyType : ((FieldInfo)member.Member).FieldType;

            return Expression.Bind(
                member.Member,
                GetCoerceOrDefault(member.Key, type, member.GraphType));
        }

        static Expression GetCoerceOrDefault(string key, Type type, IGraphType graphType)
        {
            /*
             * ValueTuple<object?, bool> value;
             * T ret;
             * value = GetOrDefaultImplementation2(dic, key);
             * if (value.Item2)
             * {
             *     ret = CoerceExpression<T>(value.Item1, member.MemberType, member.GraphType);
             * }
             * else
             * {
             *     ret = default(T);
             * }
             * return ret;
             */
            var param = Expression.Variable(typeof(ValueTuple<object?, bool>), "value");
            var ret = Expression.Variable(type, "ret");
            return Expression.Block(
                new[] { param, ret },
                Expression.Assign(
                    param,
                    Expression.Call(_getOrDefaultMethod, _dictionaryParam, Expression.Constant(key, typeof(string)))),
                Expression.IfThenElse(
                    Expression.Equal(
                        Expression.MakeMemberAccess(param, typeof(ValueTuple<object?, bool>).GetField("Item2")!),
                        Expression.Constant(true)),
                    Expression.Assign(
                        ret,
                        CoerceExpression(
                            Expression.MakeMemberAccess(param, typeof(ValueTuple<object?, bool>).GetField("Item1")!),
                            type,
                            graphType)),
                    Expression.Assign(ret, Expression.Default(type))),
                ret);
        }

        static Expression ConditionallySetMember(ParameterExpression objParam, ReflectionInfo.MemberFieldInfo member)
        {
            var type = member.Member is PropertyInfo propertyInfo ? propertyInfo.PropertyType : ((FieldInfo)member.Member).FieldType;

            /*
             * ValueTuple<object?, bool> value;
             * value = GetOrDefaultImplementation2(dic, key);
             * if (value.Item2)
             * {
             *     obj.Prop = CoerceExpression<T>(value.Item1, member.MemberType, member.GraphType);
             * }
             */
            var param = Expression.Variable(typeof(ValueTuple<object?, bool>), "value");
            return Expression.Block(
                new[] { param },
                Expression.Assign(
                    param,
                    Expression.Call(_getOrDefaultMethod, _dictionaryParam, Expression.Constant(member.Key, typeof(string)))),
                Expression.IfThen(
                    Expression.Equal(
                        Expression.MakeMemberAccess(param, typeof(ValueTuple<object?, bool>).GetField("Item2")!),
                        Expression.Constant(true)),
                    Expression.Assign(
                        Expression.MakeMemberAccess(objParam, member.Member),
                        CoerceExpression(
                            Expression.MakeMemberAccess(param, typeof(ValueTuple<object?, bool>).GetField("Item1")!),
                            type,
                            member.GraphType))));
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
                else if (_objectListTypes.Contains(type))
                {
                    elementType = typeof(object);
                }
                else if (type.IsGenericType && _genericEnumerableTypes.Contains(type.GetGenericTypeDefinition()))
                {
                    elementType = type.GetGenericArguments()[0];
                }
                else if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    elementType = type.GetInterfaces()
                        .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        ?.GetGenericArguments()[0];
                    if (elementType != null)
                    {
                        // confirm that List<T> is compatible with type
                        var listType = typeof(List<>).MakeGenericType(elementType);
                        if (!type.IsAssignableFrom(listType))
                            elementType = null;
                    }
                }
                if (elementType == null)
                    throw new InvalidOperationException($"Could not determine enumerable type for type '{type.GetFriendlyName()}' while coercing graph type '{graphType}'.");
                // create an expression that represents this:
                // (IEnumerable<object>?)expr?.Select(x => CoerceExpression(x, elementType, listGraphType.ResolvedType))
                var loopVar = Expression.Parameter(typeof(object));
                var loopContent = CoerceExpression(loopVar, elementType, elementGraphType);
                var expr2 = SelectOrNull(Expression.Convert(expr, typeof(IEnumerable)), loopVar, loopContent, isArray);
                return Expression.Convert(expr2, type);
            }

            return Expression.Call(_getPropertyValueTypedMethod.MakeGenericMethod(type), expr, Expression.Constant(type), Expression.Constant(graphType));
        }
    }

    private static readonly Type[] _objectListTypes = [
        typeof(IEnumerable),
        typeof(IList),
        typeof(ICollection),
        typeof(object),
    ];

    private static readonly Type[] _genericEnumerableTypes = [
        typeof(IEnumerable<>),
        typeof(IList<>),
        typeof(List<>),
        typeof(ICollection<>),
        typeof(IReadOnlyCollection<>),
        typeof(IReadOnlyList<>),
    ];

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

        return ret == null ? default : (T)ret;
    }

    private static Expression SelectOrNull(Expression collection, ParameterExpression loopVar, Expression loopContent, bool asArray)
    {
        var collectionVar = Expression.Variable(collection.Type, "collection");
        var enumeratorVar = Expression.Variable(typeof(IEnumerator), "enumerator");
        var getEnumeratorCall = Expression.Call(collectionVar, _getEnumeratorMethod);
        var moveNextCall = Expression.Call(enumeratorVar, _moveNextMethod);
        var getCurrent = Expression.MakeMemberAccess(enumeratorVar, _currentProperty);

        var breakLabel = Expression.Label("label1");
        var returnLabel = Expression.Label("label2");

        var listType = typeof(List<>).MakeGenericType(loopContent.Type);
        var listVariable = Expression.Variable(listType, "list");
        var returnVariable = Expression.Variable(asArray ? loopContent.Type.MakeArrayType() : listVariable.Type);
        var addMethod = listType.GetMethod(nameof(List<object>.Add))!;
        Debug.Assert(addMethod != null);
        var toArrayMethod = listType.GetMethod(nameof(List<object>.ToArray))!;
        Debug.Assert(toArrayMethod != null);

        /*
         * IEnumerable collectionVar;
         * IEnumerator enumeratorVar;
         * List<T> listVariable;
         * TList returnVariable; // TList = List<T> or T[] depending on asArray
         * 
         * collectionVar = collection;
         * returnVariable = null;
         * if (collectionVar != null)
         * {
         *     enumeratorVar = collectionVar.GetEnumerator();
         *     listVariable = new();
         *     while (true)
         *     {
         *         if (enumerator.MoveNext() == true)
         *         {
         *             T loopVar;
         *             loopVar = enumerator.Current;
         *             listVariable.Add( {loopContent} );
         *         }
         *         else
         *             break;
         *     }
         *     returnVariable = asArray ? listVariable.ToArray() : listVariable;
         * }
         * return returnVariable;
         */
        return Expression.Block(
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
    }

    private static readonly MethodInfo _getEnumeratorMethod = typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator))!;
    private static readonly MethodInfo _moveNextMethod = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!;
    private static readonly PropertyInfo _currentProperty = typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current))!;
    private static readonly ParameterExpression _dictionaryParam = Expression.Parameter(typeof(IDictionary<string, object?>));

    private static readonly MethodInfo _getOrDefaultMethod = typeof(ObjectExtensions).GetMethod(nameof(GetOrDefaultImplementation), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static ValueTuple<object?, bool> GetOrDefaultImplementation(IDictionary<string, object?> obj, string key)
        => obj.TryGetValue(key, out var value) ? ((object?, bool))(value, true) : (default, false);
}
