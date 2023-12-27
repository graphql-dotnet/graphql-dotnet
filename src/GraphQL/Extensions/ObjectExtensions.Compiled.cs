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
    /// The compiled function assumes the passed dictionary object is not <see langword="null"/>.
    /// </summary>
    public static Func<IDictionary<string, object?>, object> CompileToObject(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type sourceType,
        IInputObjectGraphType graphType)
    {
        var conv = ValueConverter.GetConversion(typeof(IDictionary<string, object?>), sourceType);
        if (conv != null)
            return conv;

        var info = GetReflectionInformation(sourceType, graphType);
        return CompileToObject(info);
    }

    /// <summary>
    /// Compiles a function to convert a dictionary to an object based on a specified <see cref="ReflectionInfo"/> instance.
    /// </summary>
    private static Func<IDictionary<string, object?>, object> CompileToObject(ReflectionInfo info)
    {
        var bestConstructor = info.Constructor;
        var ctorFields = info.CtorFields;
        var members = info.MemberFields;

        // T obj;
        var objParam = Expression.Variable(info.Type, "obj");

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
             * value = GetOrDefaultImplementation(dic, key);
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
                var isList = false;
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
                else if (type.IsGenericType)
                {
                    var genericTypeDef = type.GetGenericTypeDefinition();
                    if (genericTypeDef == typeof(List<>))
                    {
                        elementType = type.GetGenericArguments()[0];
                        isList = true;
                    }
                    else if (_genericEnumerableTypes.Contains(type.GetGenericTypeDefinition()))
                    {
                        elementType = type.GetGenericArguments()[0];
                    }
                }
                if (elementType == null)
                    throw new InvalidOperationException($"Could not determine enumerable type for CLR type '{type.GetFriendlyName()}' while coercing graph type '{graphType}'.");
                // create an expression that represents this:
                // (IEnumerable<object>?)expr?.Select(x => CoerceExpression(x, elementType, listGraphType.ResolvedType))
                Func<ParameterExpression, Expression> loopContent = (loopVar) => CoerceExpression(loopVar, elementType, elementGraphType);
                return SelectOrNull(expr, loopContent, isArray, isList, type, listGraphType);
            }

            return Expression.Call(_getPropertyValueTypedMethod.MakeGenericMethod(type), expr, Expression.Constant(graphType));
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
        typeof(ICollection<>),
        typeof(IReadOnlyCollection<>),
        typeof(IReadOnlyList<>),
    ];

    private static readonly MethodInfo _getPropertyValueTypedMethod = typeof(ObjectExtensions).GetMethod(nameof(GetPropertyValueTyped), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static T? GetPropertyValueTyped<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields)] T>(
        object? value, IGraphType mappedType)
    {
        // if the property is null return the default value
        if (value == null)
            return default;

        // Short-circuit conversion if the property value already of the right type
        // (works for converting to/from nullable value types too)
        if (typeof(T) == typeof(object) || typeof(T).IsInstanceOfType(value))
            return (T?)value;

        // in the rare circumstance that the value is not a compatible type,
        //   use the reflection-based converter for object types, and
        //   the value converter for value types

        // note that during literal/variable parsing, input object graph types
        //   are typically already converted to the correct type, as ParseDictionary
        //   is called during parsing, so this code path would not normally be hit.

        // matches only when mappedType is an input object graph type AND the value is a
        //   dictionary (not yet parsed from a dictionary into an object)
        if (value is IDictionary<string, object?> dictionary)
        {
            // unwrap non-null graph type
            mappedType = mappedType is NonNullGraphType nonNullGraphType
                ? nonNullGraphType.ResolvedType ?? throw new InvalidOperationException($"ResolvedType is null for graph type '{nonNullGraphType}'.")
                : mappedType;

            // process input object graph types
            if (mappedType is IInputObjectGraphType inputObjectGraphType)
            {
                // note that ToObject checks the ValueConverter before parsing the dictionary
                return (T)ToObject(dictionary, typeof(T), inputObjectGraphType);
            }
        }

        return ValueConverter.ConvertTo<T>(value);
    }

    private static Expression SelectOrNull(Expression collection, Func<ParameterExpression, Expression> loopContent, bool asArray, bool asList, Type returnType, IGraphType graphType)
    {
        /*
         * var collectionVar = collection;
         * TReturnType result = null;
         * if (collectionVar != null)
         * {
         *     if (collectionVar is IList && !asList)
         *     {
         *         result = (TReturnType)SelectList(collectionVar, loopVar, loopContent, asArray, asList);
         *     }
         *     else if (collectionVar is IEnumerable)
         *     {
         *         result = (TReturnType)SelectEnumerable(collectionVar, loopVar, loopContent, asArray, asList);
         *     }
         *     else
         *     {
         *         throw new InvalidOperationException($"Cannot coerce collection of type '{typeName}' to IEnumerable for graph type '{graphType}'.");
         *     }
         * }
         * return result;
         */
        var collectionVar = Expression.Variable(typeof(object), "collectionVar");
        var resultVar = Expression.Variable(returnType, "result");

        Expression enumerableCheck =
            Expression.IfThenElse(
                Expression.TypeIs(collectionVar, typeof(IEnumerable)),
                Expression.Assign(
                    resultVar,
                    Expression.Convert(
                        SelectEnumerable(Expression.Convert(collectionVar, typeof(IEnumerable)), loopContent, asArray),
                        returnType)),
                Expression.Call(_throwMethod, collectionVar, Expression.Constant(graphType, typeof(IGraphType))));

        Expression listCheck = asList ? enumerableCheck :
            Expression.IfThenElse(
                Expression.TypeIs(collectionVar, typeof(IList)),
                Expression.Assign(
                    resultVar,
                    Expression.Convert(
                        SelectList(Expression.Convert(collectionVar, typeof(IList)), loopContent),
                        returnType)),
                enumerableCheck);

        return Expression.Block(
            new[] { collectionVar, resultVar },
            Expression.Assign(collectionVar, collection),
            Expression.Assign(resultVar, Expression.Constant(null, resultVar.Type)),
            Expression.IfThen(
                Expression.NotEqual(
                    collectionVar,
                    Expression.Constant(null, collectionVar.Type)),
                listCheck),
            resultVar);
    }

    private static readonly MethodInfo _throwMethod = typeof(ObjectExtensions).GetMethod(nameof(ThrowInvalidCollectionTypeException), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static void ThrowInvalidCollectionTypeException(object collection, IGraphType graphType)
    {
        string typeName = collection?.GetType().GetFriendlyName() ?? "null";
        throw new InvalidOperationException($"Cannot coerce collection of CLR type '{typeName}' to IEnumerable for graph type '{graphType}'.");
    }

    private static Expression SelectList(Expression collection, Func<ParameterExpression, Expression> loopContentFunc)
    {
        var loopVar = Expression.Variable(typeof(object), "loopVar");
        var loopContent = loopContentFunc(loopVar);
        var countVar = Expression.Variable(typeof(int), "count");
        var collectionVar = Expression.Variable(typeof(IList), "collection");
        var indexVar = Expression.Variable(typeof(int), "i");
        var collectionCountProperty = typeof(ICollection).GetProperty(nameof(ICollection.Count))!;
        Debug.Assert(collectionCountProperty != null);

        var breakLabel = Expression.Label("breakLabel");

        var listType = loopContent.Type.MakeArrayType();
        var listVariable = Expression.Variable(listType, "list");
        var indexerProperty = typeof(IList).GetProperty("Item", [typeof(int)])!;
        Debug.Assert(indexerProperty != null);

        /*
         * IList collectionVar = collection;
         * countVar = collectionVar.Count;
         * T[] listVariable = new T[countVar];
         * index i = 0;
         * while (true)
         * {
         *     if (i < countVar)
         *     {
         *         T loopVar = collectionVar[i];
         *         listVariable[i++] = {loopContent};
         *     }
         *     else
         *     {
         *         break;
         *     }
         * }
         * return listVariable;
         */

        return Expression.Block(
            listType,
            [collectionVar, listVariable, indexVar, countVar],
            Expression.Assign(collectionVar, collection),
            Expression.Assign(countVar, Expression.Property(collectionVar, collectionCountProperty)),
            Expression.Assign(listVariable, Expression.NewArrayBounds(loopContent.Type, countVar)),
            Expression.Assign(indexVar, Expression.Constant(0, indexVar.Type)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(indexVar, countVar),
                    Expression.Block(
                        [loopVar],
                        Expression.Assign(loopVar, Expression.Property(collectionVar, indexerProperty, indexVar)),
                        Expression.Assign(
                            Expression.ArrayAccess(listVariable, Expression.PostIncrementAssign(indexVar)),
                            loopContent)),
                    Expression.Break(breakLabel)),
                breakLabel),
            listVariable);
    }

    private static Expression SelectEnumerable(Expression collection, Func<ParameterExpression, Expression> loopContentFunc, bool asArray)
    {
        var loopVar = Expression.Variable(typeof(object), "loopVar");
        var loopContent = loopContentFunc(loopVar);
        var collectionVar = Expression.Variable(typeof(IEnumerable), "collection");
        var enumeratorVar = Expression.Variable(typeof(IEnumerator), "enumerator");
        var getEnumeratorCall = Expression.Call(collectionVar, _getEnumeratorMethod);
        var moveNextCall = Expression.Call(enumeratorVar, _moveNextMethod);
        var getCurrent = Expression.MakeMemberAccess(enumeratorVar, _currentProperty);

        var breakLabel = Expression.Label("breakLabel");

        var listType = typeof(List<>).MakeGenericType(loopContent.Type);
        var listVariable = Expression.Variable(listType, "list");
        var addMethod = listType.GetMethod(nameof(List<object>.Add))!;
        Debug.Assert(addMethod != null);
        var toArrayMethod = listType.GetMethod(nameof(List<object>.ToArray))!;
        Debug.Assert(toArrayMethod != null);

        /*
         * IEnumerable collectionVar = collection;
         * IEnumerator enumeratorVar = collectionVar.GetEnumerator();
         * List<T> listVariable = new();
         * while (true)
         * {
         *     if (enumerator.MoveNext() == true)
         *     {
         *         T loopVar = enumerator.Current;
         *         listVariable.Add( {loopContent} );
         *     }
         *     else
         *         break;
         * }
         * return asArray ? listVariable.ToArray() : listVariable;
         */
        return Expression.Block(
            [collectionVar, enumeratorVar, listVariable],
            Expression.Assign(collectionVar, collection),
            Expression.Assign(enumeratorVar, getEnumeratorCall),
            Expression.Assign(listVariable, Expression.New(listVariable.Type)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.Equal(moveNextCall, Expression.Constant(true)),
                    Expression.Block(
                        [loopVar],
                        Expression.Assign(loopVar, getCurrent),
                        Expression.Call(listVariable, addMethod, loopContent)),
                    Expression.Break(breakLabel)),
                breakLabel),
            asArray ? Expression.Call(listVariable, toArrayMethod) : listVariable);
    }

    private static readonly MethodInfo _getEnumeratorMethod = typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator))!;
    private static readonly MethodInfo _moveNextMethod = typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!;
    private static readonly PropertyInfo _currentProperty = typeof(IEnumerator).GetProperty(nameof(IEnumerator.Current))!;
    private static readonly ParameterExpression _dictionaryParam = Expression.Parameter(typeof(IDictionary<string, object?>), "dic");

    private static readonly MethodInfo _getOrDefaultMethod = typeof(ObjectExtensions).GetMethod(nameof(GetOrDefaultImplementation), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static ValueTuple<object?, bool> GetOrDefaultImplementation(IDictionary<string, object?> obj, string key)
        => obj.TryGetValue(key, out var value) ? ((object?, bool))(value, true) : (default, false);
}
