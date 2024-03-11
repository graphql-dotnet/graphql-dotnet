using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Conversion;

/// <summary>
/// Dynamically constructs a list of the specified type using reflection.
/// The type must have a public constructor that takes a single parameter compatible
/// with an array of the element type, such as <see cref="IEnumerable{T}"/>,
/// or a public parameterless constructor and a public method named "Add" that
/// takes a single parameter of the element type.
/// Alternatively, the type may implement <see cref="IList"/> and have a public
/// parameterless constructor.
/// </summary>
internal sealed class CustomListConverterFactory : IListConverterFactory
{
    private static readonly MethodInfo _methodInfo;

    static CustomListConverterFactory()
    {
        Expression<Func<IEnumerable<int>>> expression = () => Enumerable.Cast<int>(null!);
        _methodInfo = ((MethodCallExpression)expression.Body).Method.GetGenericMethodDefinition();
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
            if (ctorParams.Length == 1 && ctorParams[0].ParameterType.IsAssignableFrom(enumerableElementType))
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
        // todo: directly call the constructor when GlobalSwitches.DynamicallyCompileToObject is false (?)
        var param = Expression.Parameter(typeof(object[]), "param");
        var methodInfo = _methodInfo.MakeGenericMethod(elementType);
        var castExpr = Expression.Call(methodInfo, param);
        var ctorCall = Expression.New(constructor, castExpr);
        var castCtorCall = Expression.Convert(ctorCall, typeof(object));
        var lambda = Expression.Lambda<Func<object?[], object>>(castCtorCall, param);
        return lambda.Compile();
    }

    /// <summary>
    /// Creates a delegate that calls the specified <paramref name="parameterlessCtor"/> and then
    /// adds each item in the array to the list using the specified <paramref name="addMethod"/>.
    /// </summary>
    private static Func<object?[], object> CreateLambdaViaAdd(ConstructorInfo parameterlessCtor, MethodInfo addMethod)
    {
        // todo: directly call the methods when GlobalSwitches.DynamicallyCompileToObject is false (?)
        var addMethodParameterType = addMethod.GetParameters()[0].ParameterType;
        var newExpression = Expression.New(parameterlessCtor);
        var param = Expression.Parameter(typeof(object?[]), "param");
        var instance = Expression.Variable(parameterlessCtor.DeclaringType!, "instance");
        var continueTarget = Expression.Label("continue");
        var breakTarget = Expression.Label("break");
        var index = Expression.Variable(typeof(int), "index");
        Expression addMethodExpression = Expression.ArrayIndex(param, Expression.PostIncrementAssign(index));
        if (addMethodParameterType != typeof(object))
        {
            addMethodExpression = Expression.Convert(addMethodExpression, addMethodParameterType);
        }
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
        var block = Expression.Block(
            [instance, index],
            Expression.Assign(index, Expression.Constant(0)),
            Expression.Assign(instance, newExpression),
            loop,
            Expression.Convert(instance, typeof(object))
        );
        var lambda = Expression.Lambda<Func<object?[], object>>(block, param);
        return lambda.Compile();
    }
}
