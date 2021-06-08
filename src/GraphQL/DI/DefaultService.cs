namespace GraphQL.DI
{
    public class DefaultService<T> : IDefaultService<T> where T : class
    {
        public DefaultService(T instance)
        {
            Instance = instance;
        }

        public T Instance { get; }
    }
}
