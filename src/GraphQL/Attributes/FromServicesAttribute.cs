using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL;

/// <summary>
/// Specifies that the method argument should be pulled from <see cref="IResolveFieldContext.RequestServices"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromServicesAttribute : GraphQLAttribute
{
    /// <summary>
    /// The metadata key used to store a list of the required DI-injected services for a given <see cref="QueryArgument"/>.
    /// This information can be used during schema validation to ensure that all required services have been registered.
    /// </summary>
    public const string REQUIRED_SERVICES_METADATA = "__RequiredServices__";

    /* This is a typed copy of the below code, but does not work in AOT compilation scenarios.
     * However, this is the recommended pattern for user-defined attributes.
     * 
    /// <inheritdoc/>
    public override void Modify<TParameterType>(ArgumentInformation argumentInformation)
        => argumentInformation.SetDelegate(context => context.RequestServicesOrThrow().GetRequiredService<TParameterType>());
    */

    /// <inheritdoc/>
    public override void Modify(ArgumentInformation argumentInformation)
    {
        argumentInformation.SetDelegateWithCast(context => context.RequestServicesOrThrow().GetRequiredService(argumentInformation.ParameterInfo.ParameterType));
        argumentInformation.FieldType?.DependsOn(argumentInformation.ParameterInfo.ParameterType);
    }
}
