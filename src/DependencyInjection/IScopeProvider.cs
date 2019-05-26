using System;

namespace DependencyInjection
{
    // abstraction from the method of obtaining the scope
    public interface IScopeProvider
    {
        IServiceProvider GetScopedServiceProvider(IServiceProvider rootProvider);
    }
}
