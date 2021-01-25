namespace GraphQL.Types
{
    /// <summary>
    /// Represents an input object graph type.
    /// </summary>
    public interface IInputObjectGraphType : IComplexGraphType
    {
    }

    /// <inheritdoc/>
    public class InputObjectGraphType : InputObjectGraphType<object>
    {
    }

    /// <inheritdoc cref="IInputObjectGraphType"/>
    public class InputObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IInputObjectGraphType
    {
    }
}

