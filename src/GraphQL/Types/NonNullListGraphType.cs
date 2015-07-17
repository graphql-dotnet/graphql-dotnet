namespace GraphQL.Types
{
    public class NonNullListGraphType<T> : NonNullGraphType
        where T : GraphType, new()
    {
        public NonNullListGraphType() : base(new ListGraphType<T>())
        {
        }
    }
}
