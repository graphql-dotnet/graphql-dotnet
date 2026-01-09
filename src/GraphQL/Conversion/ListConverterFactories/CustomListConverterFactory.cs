using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace GraphQL.Conversion;

/// <summary>
/// Dynamically constructs a list of the specified type using reflection.
/// The type must have a public constructor that takes a single parameter compatible
/// with <see cref="IEnumerable{T}"/>, or a public parameterless constructor and a
/// public method named "Add" that takes a single parameter of the element type.
/// Alternatively, the type may implement <see cref="IList"/> and have a public
/// parameterless constructor. The returned converter is compiled to a delegate
/// for best execution speed.
/// </summary>
/// <remarks>
/// The constructor-based approach with value types is not supported for AOT scenarios.
/// The method-based approach is supported for all types, but will be interpreted (slow)
/// for AOT scenarios.
/// </remarks>
internal sealed class CustomListConverterFactory : IListConverterFactory
{
    private static readonly MethodInfo _castMethodInfo;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
    private Type? _implementationType { get; }

    static CustomListConverterFactory()
    {
        // ensure CastOrDefault<T> is compiled for reference types in AOT scenarios
        Expression<Func<object?[], IEnumerable<object>>> expression = arr => CastOrDefault<object>(arr);
        _castMethodInfo = ((MethodCallExpression)expression.Body).Method.GetGenericMethodDefinition();
    }

    private CustomListConverterFactory()
    {
        _implementationType = null;
    }

    [RequiresUnreferencedCode(
        "For generic list types, the constructed implementation type (e.g. List<T>) must be rooted for trimming. " +
        "If the closed generic type is only referenced via reflection, the trimmer may remove its required constructors " +
        "or other members, which can cause runtime failures.")]
    public CustomListConverterFactory(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type implementationType)
    {
        if (implementationType == null)
            throw new ArgumentNullException(nameof(implementationType));
        if (implementationType.IsArray || implementationType.IsInterface || implementationType.IsAbstract)
            throw new InvalidOperationException($"Type '{implementationType.GetFriendlyName()}' is an array or interface and cannot be instantiated.");
        if (implementationType.IsGenericTypeDefinition && implementationType.GetGenericArguments().Length != 1)
            throw new InvalidOperationException($"Type '{implementationType.GetFriendlyName()}' is a generic type definition with more than one generic argument.");
        _implementationType = implementationType;
    }

    /// <summary>
    /// Returns a <see cref="CustomListConverterFactory"/> which will work for any
    /// compatible list type.
    /// </summary>
    public static CustomListConverterFactory DefaultInstance { get; } = new();

    public IListConverter Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType)
    {
        return CreateImpl(listType);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2055:Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.")]
    [UnconditionalSuppressMessage("Trimming", "IL3050: Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT")]
    public IListConverter CreateImpl(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType)
    {
        var elementType = listType.IsGenericType
            ? listType.GetGenericArguments()[0]
            : typeof(object);

        if (_implementationType != null)
        {
            listType = _implementationType.IsGenericTypeDefinition
                ? _implementationType.MakeGenericType(elementType)
                : _implementationType;
        }

        if (listType.IsArray || listType.IsInterface || listType.IsGenericTypeDefinition || listType.IsAbstract)
            throw new InvalidOperationException($"Type '{listType.GetFriendlyName()}' is an array, interface or generic type definition and cannot be instantiated.");

        var dynamicCodeCompiled =
#if NETSTANDARD2_0
            true;
#else
            System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled;
#endif

        if (!dynamicCodeCompiled && typeof(IList).IsAssignableFrom(listType))
        {
            return new ListConverter(elementType, CreateReflectionBasedLambda(listType, elementType));
        }

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
                // note: not supported for AOT scenarios as we cannot compile a reference to the CastOrDefault<T> method
                //   if T is a value type
                if (dynamicCodeCompiled || !elementType.IsValueType)
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
    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.")]
    private static MethodInfo? GetAddMethod(Type listType, Type elementType)
    {
        var addMethod = listType.GetMethod("Add", [elementType]);
        if (addMethod == null && typeof(IList).IsAssignableFrom(listType))
        {
            addMethod = typeof(IList).GetMethod("Add")!;
        }
        return addMethod;
    }

    // for AOT scenarios
    private static Func<object?[], object> CreateReflectionBasedLambda(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType, object? elementDefault)
    {
        return array =>
        {
            IList list;
            try
            {
                list = (IList)Activator.CreateInstance(listType)!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                //preserve stack trace and throw inner exception
#if NETSTANDARD2_0
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
#else
                ExceptionDispatchInfo.Throw(ex.InnerException);
#endif
                throw; //unreachable
            }
            foreach (var item in array)
            {
                list.Add(item ?? elementDefault);
            }
            return list;
        };
    }

    /// <summary>
    /// Creates a delegate that calls the specified <paramref name="constructor"/> after casting
    /// the delegate's parameter to <see cref="IEnumerable{T}"/> of the <paramref name="elementType"/>.
    /// </summary>
    private static Func<object?[], object> CreateLambdaViaConstructor(Type elementType, ConstructorInfo constructor)
    {
        var methodInfo = _castMethodInfo.MakeGenericMethod(elementType);
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
