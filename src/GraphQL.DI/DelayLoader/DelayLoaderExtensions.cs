using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.DI.DelayLoader
{
    public static class DelayLoaderExtensions
    {
        public static IDelayLoadedResult<TResult> Then<TOut, TResult>(this IDelayLoadedResult<TOut> parent, Func<TOut, Task<TResult>> func)
        {
            return new ContinueWith<TOut, TResult>(parent, func);
        }

        public static IDelayLoadedResult<TResult> Then<TOut, TResult>(this IDelayLoadedResult<TOut> parent, Func<TOut, TResult> func)
        {
            return new ContinueWith<TOut, TResult>(parent, (value) => Task.FromResult(func(value)));
        }
    }
}
