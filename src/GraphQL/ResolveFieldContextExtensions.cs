using GraphQL.Subscription;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL
{
    public static class ResolveFieldContextExtensions
    {
        /// <summary>Returns the value of the specified field argument, or defaultValue if none found</summary>
        public static TType GetArgument<TType>(this IResolveFieldContext context, string name, TType defaultValue = default)
        {
            bool exists = context.TryGetArgument(typeof(TType), name, out object result);
            return exists
                ? result == null && typeof(TType).IsValueType ? defaultValue : (TType)result
                : defaultValue;
        }

        /// <summary>Returns the value of the specified field argument, or defaultValue if none found</summary>
        public static object GetArgument(this IResolveFieldContext context, System.Type argumentType, string name, object defaultValue = null)
        {
            bool exists = context.TryGetArgument(argumentType, name, out object result);
            return exists
                ? result == null && argumentType.IsValueType ? defaultValue : result
                : defaultValue;
        }

        private static bool TryGetArgument(this IResolveFieldContext context, System.Type argumentType, string name, out object result)
        {
            var argumentName = context.Schema?.FieldNameConverter.NameFor(name, null) ?? name;

            if (context.Arguments == null || !context.Arguments.TryGetValue(argumentName, out var arg))
            {
                result = null;
                return false;
            }

            if (arg is Dictionary<string, object> inputObject)
            {
                if (argumentType == typeof(object))
                {
                    result = arg;
                    return true;
                }

                if (argumentType.IsPrimitive())
                    throw new InvalidOperationException($"Could not read primitive type '{argumentType.FullName}' from complex argument '{argumentName}'");

                result = inputObject.ToObject(argumentType);
                return true;
            }

            result = arg.GetPropertyValue(argumentType);
            return true;
        }

        /// <summary>Determines if the specified field argument has been provided in the GraphQL query request</summary>
        public static bool HasArgument(this IResolveFieldContext context, string argumentName) => context.Arguments?.ContainsKey(argumentName) ?? false;

        /// <summary>Returns the <see cref="IResolveFieldContext"/> typed as an <see cref="IResolveFieldContext{TSource}"/></summary>
        /// <exception cref="ArgumentException">Thrown if the <see cref="IResolveFieldContext.Source"/> property cannot be cast to the specified type</exception>
        public static IResolveFieldContext<TSourceType> As<TSourceType>(this IResolveFieldContext context)
        {
            if (context is IResolveFieldContext<TSourceType> typedContext)
                return typedContext;

            return new ResolveFieldContextAdapter<TSourceType>(context);
        }

        /// <summary>Returns the <see cref="IResolveEventStreamContext"/> typed as an <see cref="IResolveEventStreamContext{TSource}"/></summary>
        /// <exception cref="ArgumentException">Thrown if the <see cref="IResolveEventStreamContext.Source"/> property cannot be cast to the specified type</exception>
        public static IResolveEventStreamContext<T> As<T>(this IResolveEventStreamContext context)
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
