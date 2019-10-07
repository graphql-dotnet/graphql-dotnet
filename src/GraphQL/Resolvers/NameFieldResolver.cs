using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.Resolvers
{
    internal class NameFieldResolver : IFieldResolver
    {
        private const BindingFlags _flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

        public object Resolve(ResolveFieldContext context)
        {
            return Resolve(context?.Source, context?.FieldAst?.Name);
        }

        private static readonly ConcurrentDictionary<(Type, string), Func<object, object>> _propertyDelegates
            = new ConcurrentDictionary<(Type, string), Func<object, object>>();

        public static object Resolve(object source, string name)
        {
            if (source == null || name == null)
            {
                return null;
            }

            // We use reflection to create a delegate to access the property
            // Then cache the delegate
            // This is over 10x faster that just using reflection to get the property value

            var sourceType = source.GetType();
            var func = _propertyDelegates.GetOrAdd((sourceType, name), t => CreatePropertyDelegate(t.Item1, t.Item2));

            return func(source);
        }

        private static Func<object, object> CreatePropertyDelegate(Type target, string name)
        {
            // Get the property with reflection
            var property = target.GetProperty(name, _flags);

            if (property == null)
            {
                throw new InvalidOperationException($"Expected to find property {name} on {target.Name} but it does not exist.");
            }

            var parameter = Expression.Parameter(typeof(object), "x");
            var member = Expression.Property(Expression.Convert(parameter, target), property.Name);
            var lambda = Expression.Lambda<Func<object, object>>(Expression.Convert(member, typeof(object)), parameter);

            return lambda.Compile();
        }
    }
}
