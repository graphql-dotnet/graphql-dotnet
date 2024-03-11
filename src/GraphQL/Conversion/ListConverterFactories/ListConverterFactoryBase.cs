using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace GraphQL.Conversion;

/// <inheritdoc cref="IListConverterFactory"/>
public abstract class ListConverterFactoryBase : IListConverterFactory
{
    private readonly MethodInfo _convertMethodInfo;
    private readonly ConcurrentDictionary<Type, IListConverter> _dictionary = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ListConverterFactoryBase"/> class.
    /// </summary>
    protected ListConverterFactoryBase()
    {
        Expression<Func<object>> expression = () => GetConversion<string>();
        _convertMethodInfo = ((MethodCallExpression)expression.Body).Method.GetGenericMethodDefinition();
    }

    /// <inheritdoc/>
    public virtual IListConverter Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type listType)
    {
        // note: does not throw exceptions if there are the wrong number of generic arguments, or if an array has multiple dimensions
        var elementType = listType.IsGenericType
            ? listType.GetGenericArguments()[0]
            : listType.IsArray
            ? listType.GetElementType()!
            : typeof(object);

        return _dictionary.GetOrAdd(elementType, (Type elementType2) =>
        {
            var methodInfo = _convertMethodInfo.MakeGenericMethod(elementType2);
            try
            {
                return new ListConverter(elementType2, (Func<object?[], object>)methodInfo.Invoke(this, null)!);
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
        });
    }

    /// <summary>
    /// Returns a converter which will convert items from a given <c>object[]</c> list which contains
    /// items of the specified <typeparamref name="T"/> into a list instance.
    /// </summary>
    public abstract Func<object?[], object> GetConversion<T>();
}
