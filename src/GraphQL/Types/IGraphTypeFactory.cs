namespace GraphQL.Types;

/// <summary>
/// A factory to create instances of specific <typeparamref name="TGraphType"/> implementations.
/// </summary>
public interface IGraphTypeFactory<out TGraphType> where TGraphType : IGraphType
{
    /// <summary>
    /// Create a new instance of <typeparamref name="TGraphType"/>
    /// </summary>
    /// <returns></returns>
    public TGraphType Create();
}
