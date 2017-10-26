using System;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    public class FuncFieldResolver<TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<ResolveFieldContext, TReturnType> _resolver;

        public FuncFieldResolver(Func<ResolveFieldContext, TReturnType> resolver)
        {
            _resolver = resolver;
        }

        public TReturnType Resolve(ResolveFieldContext context)
        {
            return _resolver(context);
        }

        public bool RunThreaded()
        {
            return false;
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }

    public class FuncFieldResolver<TSourceType, TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<ResolveFieldContext<TSourceType>, TReturnType> _resolver;

        public FuncFieldResolver(Func<ResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("A resolver function must be specified");
            }
            _resolver = resolver;
        }

        public bool RunThreaded()
        {
            return false;
        }

        public TReturnType Resolve(ResolveFieldContext context)
        {
            var result = _resolver(context.As<TSourceType>());

            //most performant if available
            if (result is Task<TReturnType>)
            {
                var task = result as Task<TReturnType>;
                if (task.IsFaulted)
                {
                    throw task.Exception;
                }
                result = task.Result;
            }

            if (result is Task<object>)
            {
                var task = result as Task<object>;
                if (task.IsFaulted)
                {
                    throw task.Exception;
                }
                result = (TReturnType)task.Result;
            }

            if (result is Task)
            {
                var task = result as Task;
                if (task.IsFaulted)
                {
                    throw task.Exception;
                }
                task.ConfigureAwait(false);
                result = (TReturnType)task.GetProperyValue("Result");
            }

            return result;
        }

        object IFieldResolver.Resolve(ResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
