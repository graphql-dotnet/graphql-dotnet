using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Marks an interface CLR type's graph type as being a possible candidate for a specific object graph type or CLR type.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class PossibleTypeAttribute : GraphQLAttribute
{
    /// <summary>
    /// Marks an interface CLR type's graph type as being a possible candidate for a specific object graph type or CLR type.
    /// </summary>
    /// <param name="type">Can be an object CLR type or an object graph type.</param>
    public PossibleTypeAttribute(Type type)
    {
        if (typeof(IObjectGraphType).IsAssignableFrom(type))
        {
            ObjectGraphType = type;
        }
        else if (type.IsClass)
        {
            ObjectGraphType = type.MakeClrTypeReference(false);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Can be an object CLR type or an object graph type.");
        }
    }

    /// <summary>
    /// Returns the object graph type that is a possible type for this interface graph type.
    /// </summary>
    public Type ObjectGraphType { get; }

    /// <inheritdoc/>
    public override void Modify(IGraphType graphType)
    {
        if (graphType is not IInterfaceGraphType interfaceGraphType)
            throw new InvalidOperationException($"The {nameof(PossibleTypeAttribute)} can only be applied to graph types that derive from {nameof(InterfaceGraphType)}.");

        interfaceGraphType.Type(ObjectGraphType);
    }
}
