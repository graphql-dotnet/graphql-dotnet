namespace GraphQL
{
    public class ResolveFieldResult<T>
    {
        public T Value { get; set; }
        public bool Skip { get; set; }
    }
}
