using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies that the method argument should be pulled from <see cref="IResolveFieldContext"/>.<see cref="IProvideUserContext.UserContext">UserContext</see>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromUserContextAttribute : GraphQLAttribute
{
    /// <inheritdoc/>
    public override void Modify(ArgumentInformation argumentInformation)
    {
        argumentInformation.SetDelegateWithCast(context => context.UserContext);
    }
}
