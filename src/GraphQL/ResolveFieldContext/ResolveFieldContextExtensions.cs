using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Subscription;
using GraphQL.Types;

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
            var isIntrospection = context.ParentType == null ? context.FieldDefinition.IsIntrospectionField() : context.ParentType.IsIntrospectionType();
            var argumentName = isIntrospection ? name : (context.Schema?.NameConverter.NameForArgument(name, context.ParentType, context.FieldDefinition) ?? name);

            if (context.Arguments == null || !context.Arguments.TryGetValue(argumentName, out var arg))
            {
                result = null;
                return false;
            }

            if (arg is IDictionary<string, object> inputObject)
            {
                if (argumentType == typeof(object))
                {
                    result = arg;
                    return true;
                }

                if (argumentType.IsPrimitive())
                    throw new InvalidOperationException($"Could not read primitive type '{argumentType.FullName}' from complex argument '{argumentName}'");

                result = inputObject.ToObject(argumentType, context.FieldDefinition?.Arguments?.Find(argumentName)?.ResolvedType);
                return true;
            }

            result = arg.GetPropertyValue(argumentType);
            return true;
        }

        /// <summary>Determines if the specified field argument has been provided in the GraphQL query request</summary>
        public static bool HasArgument(this IResolveFieldContext context, string name)
        {
            var isIntrospection = context.ParentType == null ? context.FieldDefinition.IsIntrospectionField() : context.ParentType.IsIntrospectionType();
            var argumentName = isIntrospection ? name : (context.Schema?.NameConverter.NameForArgument(name, context.ParentType, context.FieldDefinition) ?? name);
            return context.Arguments?.ContainsKey(argumentName) ?? false;
        }

        /// <summary>
        /// Determines if this graph type is an introspection type
        /// </summary>
        private static bool IsIntrospectionType(this IGraphType graphType) => graphType?.Name?.StartsWith("__") ?? false;

        /// <summary>
        /// Determines if this field is an introspection field (__schema, __type, __typename) -- but not if it is a field of an introspection type
        /// </summary>
        private static bool IsIntrospectionField(this FieldType fieldType) => fieldType?.Name?.StartsWith("__") ?? false;

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
            => TryAsyncResolve<object>(context, resolve, error);

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

        private static readonly object lockExtensions = new object();
        private static readonly char[] _separators = new char[] { '.' };

        public static object GetExtension(this IResolveFieldContext context, string path)
        {
            lock (lockExtensions)
            {
             
            }
        }

        public static void SetExtension(this IResolveFieldContext context, string path, object value)
        {
            lock (lockExtensions)
            {
                string[] keys = path.Split(_separators);
                var values = context.Extensions;

                for (int i = 0; i < keys.Length - 1; ++i)
                {
                    if (values.TryGetValue(keys[i], out object v) && v is IDictionary<string, object> d)
                    {
                        values = d;
                    }
                    else
                    {
                        var temp = new Dictionary<string, object>();
                        values[keys[i]] = temp; // override value if any
                        values = temp;
                    }
                }

                values[keys[keys.Length - 1]] = value;
            }
        }
    }
}
