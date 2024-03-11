using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Conversion;

/// <summary>
/// Dynamically constructs a list of the specified type using reflection.
/// The type must have a public constructor that takes a single parameter compatible
/// with <see cref="IEnumerable{T}"/>,
/// or a public parameterless constructor and a public method named "Add" that
/// takes a single parameter of the element type. Alternatively, the type may
/// implement <see cref="IList"/> and have a public parameterless constructor.
/// The returned converter is compiled to a delegate for best execution speed.
/// </summary>
internal sealed class CustomListConverterFactory : IListConverterFactory
{
    private static readonly MethodInfo _castMethodInfo;

    static CustomListConverterFactory()
    {
        Expression<Func<IEnumerable<int>>> expression = () => CastOrDefault<int>(null!);
        _castMethodInfo = ((MethodCallExpression)expression.Body).Method.GetGenericMethodDefinition();
    }

    public IListConverter Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType)
    {
        var elementType = listType.IsGenericType
            ? listType.GetGenericArguments()[0]
            : typeof(object);

        if (listType.IsArray || listType.IsInterface || listType.IsGenericTypeDefinition)
            throw new InvalidOperationException($"Type '{listType.GetFriendlyName()}' is an array, interface or generic type definition and cannot be instantiated.");
        var ctors = listType.GetConstructors();
        ConstructorInfo? parameterlessCtor = null;
        var enumerableElementType = typeof(IEnumerable<>).MakeGenericType(elementType);

        // look for a constructor that takes a single parameter of IEnumerable<elementType>
        foreach (var ctor in ctors)
        {
            var ctorParams = ctor.GetParameters();
            if (ctorParams.Length == 1 && ctorParams[0].ParameterType == enumerableElementType)
            {
                // create expression to call the constructor
                return new ListConverter(elementType, CreateLambdaViaConstructor(elementType, ctor));
            }
            else if (ctorParams.Length == 0)
            {
                parameterlessCtor = ctor;
            }
        }

        // look for a parameterless constructor
        if (parameterlessCtor == null)
        {
            throw new InvalidOperationException($"Type '{listType.GetFriendlyName()}' is not a list type or does not have a compatible public constructor.");
        }

        // check if the type has an Add method of the proper type
        var addMethod = GetAddMethod(listType, elementType)
            ?? throw new InvalidOperationException($"Type '{listType.GetFriendlyName()}' is not a list type or does not have a compatible Add method.");

        // create expression to call the constructor and add the items one at a time
        return new ListConverter(elementType, CreateLambdaViaAdd(parameterlessCtor, addMethod));
    }

    /// <summary>
    /// Finds an 'Add' method that can be used to add items to the list.
    /// </summary>
    private static MethodInfo? GetAddMethod(Type listType, Type elementType)
    {
#pragma warning disable IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
        var addMethod = listType.GetMethod("Add", [elementType]);
#pragma warning restore IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
        if (addMethod == null && typeof(IList).IsAssignableFrom(listType))
        {
            addMethod = typeof(IList).GetMethod("Add")!;
        }
        return addMethod;
    }

    /// <summary>
    /// Creates a delegate that calls the specified <paramref name="constructor"/> after casting
    /// the delegate's parameter to <see cref="IEnumerable{T}"/> of the <paramref name="elementType"/>.
    /// </summary>
    private static Func<object?[], object> CreateLambdaViaConstructor(Type elementType, ConstructorInfo constructor)
    {
        // todo: directly call the constructor when GlobalSwitches.DynamicallyCompileToObject is false (??)

        var methodInfo = _castMethodInfo.MakeGenericMethod(elementType);
        /*
        return list =>
        {
            try
            {
                var enumerable = methodInfo.Invoke(null, [list])!;
                var list = constructor.Invoke([enumerable]);
                return list;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                //preserve stack trace and throw inner exception
#if NETSTANDARD2_0
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
#else
                ExceptionDispatchInfo.Throw(ex.InnerException);
#endif
            }
        };
        */

        var param = Expression.Parameter(typeof(object?[]), "param");
        var castExpr = Expression.Call(methodInfo, param);
        Expression body = Expression.New(constructor, castExpr);
        if (body.Type.IsValueType)
            body = Expression.Convert(body, typeof(object));
        var lambda = Expression.Lambda<Func<object?[], object>>(body, param);
        return lambda.Compile();
    }

    /// <summary>
    /// Creates a delegate that calls the specified <paramref name="parameterlessCtor"/> and then
    /// adds each item in the array to the list using the specified <paramref name="addMethod"/>.
    /// </summary>
    private static Func<object?[], object> CreateLambdaViaAdd(ConstructorInfo parameterlessCtor, MethodInfo addMethod)
    {
        // todo: directly call the methods when GlobalSwitches.DynamicallyCompileToObject is false (??)
        var addMethodParameterType = addMethod.GetParameters()[0].ParameterType;
        var newExpression = Expression.New(parameterlessCtor);
        var param = Expression.Parameter(typeof(object?[]), "param");
        var instance = Expression.Variable(parameterlessCtor.DeclaringType!, "instance");
        var continueTarget = Expression.Label("continue");
        var breakTarget = Expression.Label("break");
        var index = Expression.Variable(typeof(int), "index");

        // create expression to obtain the item from the array
        var indexExpression = Expression.ArrayIndex(param, Expression.PostIncrementAssign(index));
        Expression addMethodExpression;
        if (!addMethodParameterType.IsValueType)
        {
            // cast the item to the proper type (if not already object type)
            addMethodExpression = addMethodParameterType != typeof(object)
                ? Expression.Convert(indexExpression, addMethodParameterType)
                : indexExpression;
        }
        else
        {
            // cast the item to the proper type, or use default(T) if null
            var blockParam = Expression.Parameter(typeof(object), "value");
            addMethodExpression = Expression.Block(
                [blockParam],
                Expression.Assign(blockParam, indexExpression),
                Expression.Condition(
                    Expression.Equal(blockParam, Expression.Constant(null)),
                    Expression.Default(addMethodParameterType),
                    Expression.Convert(blockParam, addMethodParameterType)));
        }

        // create loop to add each item to the list
        var loop = Expression.Loop(
            Expression.IfThenElse(
                Expression.LessThan(index, Expression.ArrayLength(param)),
                Expression.Block(
                    Expression.Call(instance, addMethod, addMethodExpression),
                    Expression.Continue(continueTarget)
                ),
                Expression.Break(breakTarget)
            ),
            breakTarget,
            continueTarget
        );

        // create block to initialize the list, add each item, and return the list
        var block = Expression.Block(
            [instance, index],
            Expression.Assign(index, Expression.Constant(0)),
            Expression.Assign(instance, newExpression),
            loop,
            Expression.Convert(instance, typeof(object))
        );

        // create lambda and compile it
        var lambda = Expression.Lambda<Func<object?[], object>>(block, param);
        return lambda.Compile();
    }

    /// <summary>
    /// Casts each item in the array to the specified type, returning the default value for null items.
    /// </summary>
    private static IEnumerable<T> CastOrDefault<T>(object?[] source)
    {
        for (var i = 0; i < source.Length; i++)
        {
            var value = source[i];
            yield return value == null ? default! : (T)value;
        }
    }
}
