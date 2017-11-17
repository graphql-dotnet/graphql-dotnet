namespace GraphQL.Language.AST
{
    public interface IValue : INode
    {
        object Value { get; }
    }

    public interface IValue<T> : IValue
    {
        new T Value { get; }
    }
}
