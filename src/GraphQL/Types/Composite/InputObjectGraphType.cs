namespace GraphQL.Types
{
    public interface IInputObjectGraphType : IComplexGraphType
    {
    }

    public class InputObjectGraphType : InputObjectGraphType<object>
    {
    }

    public class InputObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IInputObjectGraphType
    {
    }
}

