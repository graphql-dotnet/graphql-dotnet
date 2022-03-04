using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Provides extension methods for <see cref="IResolveFieldContext"/> instances.
    /// </summary>
    public static class ResolveFieldContextExtensions
    {
        /// <summary>
        /// Determines if currently executed field has any directives provided in the GraphQL query request.
        /// </summary>
        public static bool HasDirectives(this IResolveFieldContext context)
        {
            return context.Directives?.Count > 0;
        }

        /// <summary>
        /// Determines if the specified directive has been provided in the GraphQL query request for currently executed field.
        /// </summary>
        public static bool HasDirective(this IResolveFieldContext context, string name)
        {
            return context.Directives != null && context.Directives.ContainsKey(name);
        }

        /// <summary>
        /// Gets directive provided in the GraphQL query request by its name.
        /// </summary>
        public static DirectiveInfo? GetDirective(this IResolveFieldContext context, string name)
        {
            return context.Directives != null && context.Directives.TryGetValue(name, out var value)
                ? value
                : null;
        }

        /// <summary>
        /// Returns the value of the specified field argument, or <paramref name="defaultValue"/> when unspecified or when specified as <see langword="null"/>.
        /// Field and variable default values take precedence over the <paramref name="defaultValue"/> parameter.
        /// </summary>
        public static TType GetArgument<TType>(this IResolveFieldContext context, string name, TType defaultValue = default!)
        {
            bool exists = context.TryGetArgument(typeof(TType), name, out object? result);
            return exists
                ? result == null ? defaultValue : (TType)result
                : defaultValue;
        }

        /// <inheritdoc cref="GetArgument{TType}(IResolveFieldContext, string, TType)"/>
        public static object? GetArgument(this IResolveFieldContext context, Type argumentType, string name, object? defaultValue = null)
        {
            bool exists = context.TryGetArgument(argumentType, name, out object? result);
            return exists
                ? result ?? defaultValue
                : defaultValue;
        }

        internal static bool TryGetArgument(this IResolveFieldContext context, Type argumentType, string name, out object? result)
        {
            var isIntrospection = context.ParentType == null ? context.FieldDefinition.IsIntrospectionField() : context.ParentType.IsIntrospectionType();
            var argumentName = isIntrospection ? name : (context.Schema?.NameConverter.NameForArgument(name, context.ParentType!, context.FieldDefinition) ?? name);

            if (context.Arguments == null || !context.Arguments.TryGetValue(argumentName, out var arg))
            {
                result = null;
                return false;
            }

            if (arg.Value is IDictionary<string, object?> inputObject)
            {
                if (argumentType == typeof(object))
                {
                    result = arg.Value;
                    return true;
                }

                result = inputObject.ToObject(argumentType, context.FieldDefinition?.Arguments?.Find(argumentName)?.ResolvedType);
                return true;
            }

            result = arg.Value.GetPropertyValue(argumentType, context.FieldDefinition?.Arguments?.Find(argumentName)?.ResolvedType);
            return true;
        }

        /// <summary>Determines if the specified field argument has been provided in the GraphQL query request.</summary>
        public static bool HasArgument(this IResolveFieldContext context, string name)
        {
            var isIntrospection = context.ParentType == null ? context.FieldDefinition.IsIntrospectionField() : context.ParentType.IsIntrospectionType();
            var argumentName = isIntrospection ? name : (context.Schema?.NameConverter.NameForArgument(name, context.ParentType!, context.FieldDefinition) ?? name);
            return context.Arguments != null && context.Arguments.TryGetValue(argumentName, out var value) && value.Source != ArgumentSource.FieldDefault;
        }

        /// <summary>
        /// Determines if this field is an introspection field (__schema, __type, __typename) -- but not if it is a field of an introspection type.
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

        private static readonly char[] _separators = { '.' };

        /// <summary>
        /// Thread safe method to get value by path (key1.key2.keyN) from output extensions dictionary.
        /// </summary>
        /// <param name="context">Context with extensions response map.</param>
        /// <param name="path">Path to value in key1.key2.keyN format.</param>
        /// <returns>Value, if any exists on the specified path, otherwise <c>null</c>.</returns>
        public static object? GetOutputExtension(this IResolveFieldContext context, string path)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.OutputExtensions == null || context.OutputExtensions.Count == 0)
                return null;

            lock (context.OutputExtensions)
            {
                var values = context.OutputExtensions;

                if (path.IndexOf('.') != -1)
                {
                    string[] keys = path.Split(_separators);

                    for (int i = 0; i < keys.Length - 1; ++i)
                    {
                        if (values.TryGetValue(keys[i], out object? v) && v is IDictionary<string, object?> d)
                            values = d;
                        else
                            return null;
                    }

                    return values.TryGetValue(keys[keys.Length - 1], out object? result) ? result : null;
                }
                else
                {
                    return values.TryGetValue(path, out object? result) ? result : null;
                }
            }
        }

        /// <summary>
        /// Thread safe method to set value by path (key1.key2.keyN) to output extensions dictionary.
        /// if the given path or its part contains values, then they will be overwritten.
        /// </summary>
        /// <param name="context">Context with extensions response map.</param>
        /// <param name="path">Path to value in key1.key2.keyN format.</param>
        /// <param name="value">Value to set.</param>
        public static void SetOutputExtension(this IResolveFieldContext context, string path, object? value)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.OutputExtensions == null)
                throw new ArgumentException("Extensions property is null", nameof(context));

            lock (context.OutputExtensions)
            {
                var values = context.OutputExtensions;

                if (path.IndexOf('.') != -1)
                {
                    string[] keys = path.Split(_separators);

                    for (int i = 0; i < keys.Length - 1; ++i)
                    {
                        if (values.TryGetValue(keys[i], out object? v) && v is IDictionary<string, object?> d)
                        {
                            values = d;
                        }
                        else
                        {
                            var temp = new Dictionary<string, object?>();
                            values[keys[i]] = temp; // overwrite value if any
                            values = temp;
                        }
                    }

                    values[keys[keys.Length - 1]] = value;
                }
                else
                {
                    values[path] = value;
                }
            }
        }

        /// <summary>
        /// Make a copy of the specified <see cref="IResolveFieldContext"/> instance so it can be
        /// accessed at a later time.
        /// </summary>
        public static IResolveFieldContext Copy(this IResolveFieldContext context) => new ResolveFieldContext(context);

        /// <summary>
        /// Make a copy of the specified <see cref="IResolveFieldContext"/> instance so it can be
        /// accessed at a later time.
        /// </summary>
        public static IResolveFieldContext<TSource> Copy<TSource>(this IResolveFieldContext<TSource> context) => new ResolveFieldContext<TSource>(context);
    }
}
