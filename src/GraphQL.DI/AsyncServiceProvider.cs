using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GraphQL.DI
{
    public static class AsyncServiceProvider
    {
        private static AsyncLocal<IServiceProvider> _currentServiceProvider = new AsyncLocal<IServiceProvider>();

        public static IServiceProvider Current
        {
            get
            {
                return _currentServiceProvider.Value;
            }
            set
            {
                _currentServiceProvider.Value = value;
            }
        }

        public static T GetRequiredService<T>() {
            return (T)GetRequiredService(typeof(T));
        }

        public static object GetRequiredService(Type t)
        {
            var provider = Current ?? throw new InvalidOperationException("No service provider defined in this context");
            return provider.GetService(t) ?? throw new InvalidOperationException("No service registered of type " + t.Name);
        }
    }
}
