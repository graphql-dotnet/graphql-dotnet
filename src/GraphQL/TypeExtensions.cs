namespace GraphQL
{
    public static class TypeExtensions
    {
        public static T As<T>(this object item)
            where T : class
        {
            return item as T;
        }
    }
}
