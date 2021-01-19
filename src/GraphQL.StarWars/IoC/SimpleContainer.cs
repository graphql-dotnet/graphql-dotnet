using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.StarWars.IoC
{
    public interface ISimpleContainer : IDisposable
    {
        object Get(Type serviceType);
        T Get<T>();
        void Register<TService>() where TService : class;
        void Register<TService>(Func<TService> instanceCreator) where TService : class;
        void Register<TService, TImpl>() where TService : class where TImpl : class, TService;
        void Singleton<TService>(TService instance) where TService : class;
        void Singleton<TService>(Func<TService> instanceCreator) where TService : class;
    }

    public class SimpleContainer : ISimpleContainer
    {
        private readonly Dictionary<Type, Func<object>> _registrations = new Dictionary<Type, Func<object>>();

        public void Register<TService>() where TService : class
        {
            Register<TService, TService>();
        }

        public void Register<TService, TImpl>() where TService : class where TImpl : class, TService
        {
            _registrations.Add(typeof(TService),
                () =>
                {
                    var implType = typeof(TImpl);
                    return typeof(TService) == implType
                        ? CreateInstance(implType)
                        : Get(implType);
                });
        }

        public void Register<TService>(Func<TService> instanceCreator) where TService : class
        {
            _registrations.Add(typeof(TService), () => instanceCreator());
        }

        public void Singleton<TService>(TService instance) where TService : class
        {
            _registrations.Add(typeof(TService), () => instance);
        }

        public void Singleton<TService>(Func<TService> instanceCreator) where TService : class
        {
            var lazy = new Lazy<TService>(instanceCreator);
            Register(() => lazy.Value);
        }

        public T Get<T>() => (T)Get(typeof(T));

        public object Get(Type serviceType)
        {
            if (_registrations.TryGetValue(serviceType, out var creator))
            {
                return creator();
            }

            if (!serviceType.IsAbstract)
            {
                return CreateInstance(serviceType);
            }

            throw new InvalidOperationException("No registration for " + serviceType);
        }

        public void Dispose()
        {
            _registrations.Clear();
        }

        private object CreateInstance(Type implementationType)
        {
            var ctor = implementationType.GetConstructors().OrderByDescending(x => x.GetParameters().Length).First();
            var parameterTypes = ctor.GetParameters().Select(p => p.ParameterType);
            var dependencies = parameterTypes.Select(Get).ToArray();
            return Activator.CreateInstance(implementationType, dependencies);
        }
    }
}
