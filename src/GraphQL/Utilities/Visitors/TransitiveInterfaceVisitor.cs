using GraphQL.Types;

namespace GraphQL.Utilities;

/// <summary>
/// A schema visitor that adds direct references to any transitively implemented interfaces
/// that are not already directly implemented.
/// </summary>
public sealed class TransitiveInterfaceVisitor : BaseSchemaNodeVisitor
{
    private TransitiveInterfaceVisitor()
    {
    }

    /// <inheritdoc cref="TransitiveInterfaceVisitor"/>
    public static TransitiveInterfaceVisitor Instance { get; } = new();

    /// <inheritdoc/>
    public override void VisitObject(IObjectGraphType type, ISchema schema)
    {
        AddTransitiveInterfaces(type);
    }

    /// <inheritdoc/>
    public override void VisitInterface(IInterfaceGraphType type, ISchema schema)
    {
        AddTransitiveInterfaces(type);
    }

    private void AddTransitiveInterfaces(IImplementInterfaces type)
    {
        if (type.ResolvedInterfaces.Count == 0)
            return;

        var checkedInterfaces = new HashSet<IInterfaceGraphType>();
        var transitiveInterfaces = new HashSet<IInterfaceGraphType>();
        FindTransitiveInterfaces(type, type, transitiveInterfaces, checkedInterfaces);

        // Add any transitive interfaces that aren't already directly implemented
        foreach (var iface in transitiveInterfaces)
        {
            if (!type.ResolvedInterfaces.Contains(iface))
            {
                type.AddResolvedInterface(iface);
            }
        }
    }

    private void FindTransitiveInterfaces(IImplementInterfaces baseType, IImplementInterfaces type, HashSet<IInterfaceGraphType> transitiveInterfaces, HashSet<IInterfaceGraphType> checkedInterfaces)
    {
        foreach (var iface in type.ResolvedInterfaces.List)
        {
            if (checkedInterfaces.Add(iface))
            {
                foreach (var transitiveInterface in iface.ResolvedInterfaces.List)
                {
                    if (transitiveInterface == baseType)
                        throw new InvalidOperationException($"'{baseType.Name}' cannot implement interface '{iface.Name}' because it creates a circular reference.");

                    transitiveInterfaces.Add(transitiveInterface);
                }
                FindTransitiveInterfaces(baseType, iface, transitiveInterfaces, checkedInterfaces);
            }
        }
    }
}
