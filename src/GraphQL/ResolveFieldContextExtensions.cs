using GraphQL.Subscription;
using GraphQL.Types;
using System;
using System.Collections.Generic;

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
            if (context is ResolveFieldContext<TSourceType> typedContext)
                return typedContext;

            return new ResolveFieldContext<TSourceType>(context);
        }

        internal static IResolveEventStreamContext<T> As<T>(this IResolveEventStreamContext context)
        {
            if (context is ResolveEventStreamContext<T> typedContext)
                return typedContext;

            return new ResolveEventStreamContext<T>(context);
        }
    }
}
