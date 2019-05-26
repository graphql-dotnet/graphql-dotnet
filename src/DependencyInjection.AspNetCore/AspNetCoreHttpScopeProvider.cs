using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DependencyInjection.AspNetCore
{
    // Scope Provider fo HttpContext
    public class AspNetCoreHttpScopeProvider : IScopeProvider
    {
        public IServiceProvider GetScopedServiceProvider(IServiceProvider root) => root.GetService<IHttpContextAccessor>()?.HttpContext?.RequestServices;
    }
}
