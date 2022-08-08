using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Marks the type as implementing a specified property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ImplementsAttribute : GraphQLAttribute
    {
        /// <summary>
        /// Marks the type as implementing a specified property.
        /// </summary>
        /// <param name="type">Can be a interface CLR type or an interface graph type.</param>
        public ImplementsAttribute(Type type)
        {
            if (typeof(IInterfaceGraphType).IsAssignableFrom(type))
            {
                InterfaceGraphType = type;
            }
            else if (type.IsInterface)
            {
                InterfaceGraphType = typeof(GraphQLClrOutputTypeReference<>).MakeGenericType(type);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        /// <summary>
        /// Returns the interface graph type that this graph type implements.
        /// </summary>
        public Type InterfaceGraphType { get; }

        /// <inheritdoc/>
        public override void Modify(IGraphType graphType)
        {
            if (graphType is not IImplementInterfaces graphType2)
                throw new InvalidOperationException("The InterfaceAttribute can only be applied to graph types that implement IImplementInterfaces.");

            graphType2.Interfaces.Add(InterfaceGraphType);
        }
    }
}
