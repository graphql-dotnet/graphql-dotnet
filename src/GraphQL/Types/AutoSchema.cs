using GraphQL.Utilities;

namespace GraphQL.Types;

/// <summary>
/// A schema with a Query type that is initialized to an instance
/// of <see cref="AutoRegisteringObjectGraphType{TSourceType}"/>
/// with <typeparamref name="TQueryClrType"/> as the query clr type.
/// </summary>
public class AutoSchema<[NotAGraphType] TQueryClrType> : Schema
{
    /// <summary>
    /// Initializes a new instance from the specified service provider.
    /// </summary>
    [RequiresUnreferencedCode("Includes various functionality which does not statically reference members.")]
    [RequiresDynamicCode("Builds resolvers at runtime, requiring dynamic code generation.")]
    public AutoSchema(IServiceProvider serviceProvider) : base(serviceProvider, true)
    {
        Query = serviceProvider.GetRequiredService<AutoRegisteringObjectGraphType<TQueryClrType>>();
    }
}
