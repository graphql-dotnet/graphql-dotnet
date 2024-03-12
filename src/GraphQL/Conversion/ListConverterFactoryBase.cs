using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace GraphQL.Conversion;

/// <inheritdoc cref="IListConverterFactory"/>
public abstract class ListConverterFactoryBase : IListConverterFactory
{
    private readonly MethodInfo _convertMethodInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListConverterFactoryBase"/> class.
    /// </summary>
    protected ListConverterFactoryBase()
    {
        Expression<Func<object>> expression = () => Create<string>();
        _convertMethodInfo = ((MethodCallExpression)expression.Body).Method.GetGenericMethodDefinition();
    }

    /// <inheritdoc/>
    public virtual IListConverter Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType)
    {
        var elementType = GetElementType(listType);

        var methodInfo = _convertMethodInfo.MakeGenericMethod(elementType);
        try
        {
            return new ListConverter(elementType, (Func<object?[], object>)methodInfo.Invoke(this, null)!);
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
    }

    /// <summary>
    /// Returns the element type of the specified list type.
    /// </summary>
    protected Type GetElementType(Type listType)
    {
        if (listType is null)
            throw new ArgumentNullException(nameof(listType));
        if (listType.IsGenericTypeDefinition)
            throw new InvalidOperationException($"Type '{listType.GetFriendlyName()}' is a generic type definition and the element type cannot be determined.");
        if (listType.IsGenericType)
        {
            var genericArguments = listType.GetGenericArguments();
            if (genericArguments.Length != 1)
                throw new InvalidOperationException($"Type '{listType.GetFriendlyName()}' is a generic type with {genericArguments.Length} generic arguments so the element type cannot be determined.");
            return genericArguments[0];
        }
        return listType.IsArray
            ? listType.GetElementType()!
            : typeof(object);
    }

    /// <summary>
    /// Returns a converter which will convert items from a given <c>object[]</c> list which contains
    /// items of the specified elment type <typeparamref name="T"/> into a list instance.
    /// </summary>
    public abstract Func<object?[], object> Create<T>();
}
