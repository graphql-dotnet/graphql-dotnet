namespace GraphQL.Types;

/// <summary>
/// Represents an interface for all complex (that is, having their own properties) input and output graph types.
/// </summary>
public interface IComplexGraphType : IGraphType
{
}

/// <summary>
/// Extensions for <see cref="IComplexGraphType"/>.
/// </summary>
public static class ComplexGraphTypeExtensions
{
    /// <summary>
    /// Returns a set of fields configured for any <see cref="IComplexGraphType"/>.
    /// </summary>
    public static ITypeFields Fields(this IComplexGraphType type)
    {
        return type switch
        {
            IObjectGraphType obj => obj.Fields,
            IInterfaceGraphType iface => iface.Fields,
            IInputObjectGraphType input => input.Fields,
            _ => throw new NotSupportedException()
        };
    }
}
