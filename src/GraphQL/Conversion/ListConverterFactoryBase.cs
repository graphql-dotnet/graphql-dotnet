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
    [RequiresUnreferencedCode("Uses reflection to access generic method information which may be trimmed.")]
    [RequiresDynamicCode("Uses reflection to create generic method signatures.")]
    protected ListConverterFactoryBase()
    {
        Expression<Func<object>> expression = () => Create<string>();
        _convertMethodInfo = ((MethodCallExpression)expression.Body).Method.GetGenericMethodDefinition();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "The constructor is marked with RequiresDynamicCode.")]
    public virtual IListConverter Create(Type listType)
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
    protected virtual Type GetElementType(Type listType) => listType.GetListElementType();

    /// <summary>
    /// Returns a converter which will convert items from a given <c>object[]</c> list which contains
    /// items of the specified elment type <typeparamref name="T"/> into a list instance.
    /// </summary>
    public abstract Func<object?[], object> Create<T>();
}
