using GraphQL.Types;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace GraphQL.Resolvers
{
    public class FieldResolverWrapper : IFieldResolverInternal
    {
        private readonly IFieldResolver _resolver;
        public FieldResolverWrapper(IFieldResolver resolver) => _resolver = resolver;
        Task IFieldResolverInternal.SetResultAsync(IResolveFieldContext context)
        {
            var result = _resolver.Resolve(context);
            if (result is Task task)
            {
                if (task.IsCompleted)
                {
                    context.Result = task.GetResult();
                    return Task.CompletedTask;
                }
                else
                {
                    Func<Task> func = (async () => { await task; context.Result = task.GetResult(); });
                    return func();
                }
            }
            else
            {
                context.Result = result;
                return Task.CompletedTask;
            }
        }
    }
}
