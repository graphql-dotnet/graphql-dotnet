using System;
using GraphQL.StarWars.IoC;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests
{
    public class MsDiContainer : ISimpleContainer
    {
        private readonly ServiceCollection _services = new ServiceCollection();
        private ServiceProvider _provider;

        private void AssertNotCreated()
        {
            if (_provider != null)
                throw new InvalidOperationException();
        }

        public object Get(Type serviceType)
        {
            _provider ??= _services.BuildServiceProvider();
            return _provider.GetService(serviceType);
        }

        public T Get<T>()
        {
            _provider ??= _services.BuildServiceProvider();
            return _provider.GetService<T>();
        }

        object IServiceProvider.GetService(Type serviceType)
            => (_provider ??= _services.BuildServiceProvider()).GetService(serviceType);

        public void Register<TService>() where TService : class
        {
            AssertNotCreated();
            _services.AddTransient<TService>();
        }

        public void Register<TService>(Func<TService> instanceCreator) where TService : class
        {
            AssertNotCreated();
            _services.AddTransient(provider => instanceCreator());
        }

        public void Register<TService, TImpl>() where TService : class where TImpl : class, TService
        {
            AssertNotCreated();
            _services.AddTransient<TService, TImpl>();
        }

        public void Singleton<TService>() where TService : class
        {
            AssertNotCreated();
            _services.AddSingleton<TService>();
        }

        public void Singleton<TService>(TService instance) where TService : class
        {
            AssertNotCreated();
            _services.AddSingleton(instance);
        }

        public void Singleton<TService>(Func<TService> instanceCreator) where TService : class
        {
            AssertNotCreated();
            _services.AddSingleton(provider => instanceCreator());
        }

        public void Dispose()
        {
            _provider.Dispose();
            _provider = null;
        }
    }
}
