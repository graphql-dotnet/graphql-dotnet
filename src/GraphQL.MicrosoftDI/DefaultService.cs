using System;
using GraphQL.DI;

namespace GraphQL.MicrosoftDI
{
    /// <summary>
    /// Returns an instance of <see cref="IDefaultService{T}"/> which contains a newly created
    /// instance of <typeparamref name="T"/> in the <see cref="IDefaultService{T}.Instance"/> property.
    /// The instance is not pulled from dependency injection, but DI is used to supply the constructor
    /// parameters for the class. Does not support classes that implement <see cref="IDisposable"/>.
    /// </summary>
    internal sealed class DefaultServiceFromDI<T> : IDefaultService<T> where T : class
    {
        public DefaultServiceFromDI(IServiceProvider serviceProvider)
        {
            Instance = Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance<T>(serviceProvider);
        }

        public T Instance { get; }
    }
}
