using GraphQL.Subscription;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL
{
    public static class ResolveFieldContextExtensions
    {
        public static TType GetArgument<TType>(this IResolveFieldContext context, string name, TType defaultValue = default)
        {
            return (TType)context.GetArgument(typeof(TType), name, defaultValue);
        }

        public static object GetArgument(this IResolveFieldContext context, System.Type argumentType, string name, object defaultValue = null)
        {
            var argumentName = context.Schema?.FieldNameConverter.NameFor(name, null) ?? name;

            if (context.Arguments == null || !context.Arguments.TryGetValue(argumentName, out var arg))
            {
                return defaultValue;
            }

            if (arg is Dictionary<string, object> inputObject)
            {
                if (argumentType == typeof(object))
                    return arg;

                if (argumentType.IsPrimitive())
                    throw new InvalidOperationException($"Could not read primitive type '{argumentType.FullName}' from complex argument '{argumentName}'");

                return inputObject.ToObject(argumentType);
            }

            var result = arg.GetPropertyValue(argumentType);

            return result == null && argumentType.IsValueType ? defaultValue : result;
        }

        public static bool HasArgument(this IResolveFieldContext context, string argumentName)
        {
            return context.Arguments?.ContainsKey(argumentName) ?? false;
        }

        internal static IResolveFieldContext<TSourceType> As<TSourceType>(this IResolveFieldContext context)
        {
            if (context is IResolveFieldContext<TSourceType> typedContext)
                return typedContext;

            return new ResolveFieldContextAdapter<TSourceType>(context);
        }

        internal static IResolveEventStreamContext<T> As<T>(this IResolveEventStreamContext context)
        {
            if (context is IResolveEventStreamContext<T> typedContext)
                return typedContext;

            return new ResolveEventStreamContext<T>(context);
        }

        public static Task<object> TryAsyncResolve(this IResolveFieldContext context, Func<IResolveFieldContext, Task<object>> resolve, Func<ExecutionErrors, Task<object>> error = null)
        {
            return TryAsyncResolve<object>(context, resolve, error);
        }

        public static async Task<TResult> TryAsyncResolve<TResult>(this IResolveFieldContext context, Func<IResolveFieldContext, Task<TResult>> resolve, Func<ExecutionErrors, Task<TResult>> error = null)
        {
            try
            {
                return await resolve(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (error == null)
                {
                    var er = new ExecutionError(ex.Message, ex);
                    er.AddLocation(context.FieldAst, context.Document);
                    er.Path = context.Path;
                    context.Errors.Add(er);
                    return default;
                }
                else
                {
                    var result = error(context.Errors);
                    return result == null ? default : await result.ConfigureAwait(false);
                }
            }
        }
    }
}
