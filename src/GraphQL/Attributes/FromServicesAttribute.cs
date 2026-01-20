using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL;

/// <summary>
/// Specifies that the method argument should be pulled from <see cref="IResolveFieldContext.RequestServices"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromServicesAttribute : ParameterAttribute
{
    /// <summary>
    /// The metadata key used to store a list of the required DI-injected services for a given <see cref="QueryArgument"/>.
    /// This information can be used during schema validation to ensure that all required services have been registered.
    /// The type of the metadata value is <see cref="List{Type}"/>.
    /// </summary>
    public const string REQUIRED_SERVICES_METADATA = "__RequiredServices__";

    /// <inheritdoc/>
    public override Func<IResolveFieldContext, T> GetResolver<T>(ArgumentInformation argumentInformation)
    {
        argumentInformation.FieldType?.DependsOn(typeof(T));
        return context => context.RequestServicesOrThrow().GetRequiredService<T>();
    }
}
