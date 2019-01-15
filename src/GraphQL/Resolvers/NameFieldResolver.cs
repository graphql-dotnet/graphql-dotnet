using System;
using System.Collections.Concurrent;
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

            // Use reflection to call the method to generate our delegate
            MethodInfo constructedHelper = delegateHelperMethod.MakeGenericMethod(
                property.DeclaringType, property.GetMethod.ReturnType);

            return (Func<object, object>)constructedHelper.Invoke(null, new object[] { property });
        }

        private static readonly MethodInfo delegateHelperMethod = typeof(NameFieldResolver).GetMethod(nameof(DelegateHelper),
            BindingFlags.Static | BindingFlags.NonPublic);

        private static Func<object, object> DelegateHelper<TTarget, TReturn>(PropertyInfo property)
        {
            // Convert the slow MethodInfo into a fast, strongly typed, open delegate
            Func<TTarget, TReturn> func = (Func<TTarget, TReturn>)
                Delegate.CreateDelegate(typeof(Func<TTarget, TReturn>), property.GetMethod);

            // Now create a more weakly typed delegate which will call the strongly typed one
            return (object target) => func((TTarget)target);
        }
    }
}
