using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL;

/// <summary>
/// Specifies that the method argument should be pulled from <see cref="IResolveFieldContext.RequestServices"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromServicesAttribute : GraphQLAttribute
{
    /* This is a typed copy of the below code, but does not work in AOT compilation scenarios.
     * However, this is the recommended pattern for user-defined attributes.
     * 
    /// <inheritdoc/>
    public override void Modify<TParameterType>(ArgumentInformation argumentInformation)
        => argumentInformation.SetDelegate(context => context.RequestServicesOrThrow().GetRequiredService<TParameterType>());
    */

    /// <inheritdoc/>
    public override void Modify(ArgumentInformation argumentInformation)
        => argumentInformation.SetDelegateWithCast(context => context.RequestServicesOrThrow().GetRequiredService(argumentInformation.ParameterInfo.ParameterType));
}
