using System;

namespace GraphQL
{
    public interface IDependencyResolver
    {
        T Resolve<T>();
        object Resolve(Type type);
    }

    public class DefaultDependencyResolver : IDependencyResolver
    {
        public T Resolve<T>()
        {
            return (T) Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            return Activator.CreateInstance(type);
        }
    }

    public class FuncDependencyResolver : IDependencyResolver
    {
        private readonly Func<Type, object> _resolver;

        public FuncDependencyResolver(Func<Type, object> resolver)
        {
            _resolver = resolver;
        }

        public T Resolve<T>()
        {
            return (T) Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            return _resolver(type);
        }
    }
}
