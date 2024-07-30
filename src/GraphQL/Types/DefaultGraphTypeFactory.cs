namespace GraphQL.Types;

/// <summary>
/// A generic factory to create instances of specific <typeparamref name="TGraphType"/> implementations from parameterless constructors.
/// </summary>
public class DefaultGraphTypeFactory<[RequireParameterlessConstructor] TGraphType> : IGraphTypeFactory<TGraphType> where TGraphType : IGraphType
{
    /// <summary>
    /// Create a new instance of <typeparamref name="TGraphType"/>. Requires a parameterless constructor.
    /// </summary>
    /// <returns></returns>
    public TGraphType Create() => Activator.CreateInstance<TGraphType>();
}
