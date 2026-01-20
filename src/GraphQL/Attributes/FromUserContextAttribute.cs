using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies that the method argument should be pulled from <see cref="IResolveFieldContext"/>.<see cref="IProvideUserContext.UserContext">UserContext</see>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromUserContextAttribute : ParameterAttribute
{
    /// <inheritdoc/>
    public override Func<IResolveFieldContext, T> GetResolver<T>(ArgumentInformation argumentInformation)
    {
        return context => (T)context.UserContext!;
    }
}
