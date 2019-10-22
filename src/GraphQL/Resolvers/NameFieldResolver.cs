using GraphQL.Types;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace GraphQL.Resolvers
{
    internal class NameFieldResolver : IFieldResolver
    {
        private static readonly ConcurrentDictionary<(Type targetType, string propertyName), Func<object, object>> _propertyDelegates
            = new ConcurrentDictionary<(Type, string), Func<object, object>>();

        private NameFieldResolver() { }

        public static NameFieldResolver Instance { get; } = new NameFieldResolver();

        public object Resolve(ResolveFieldContext context) => Resolve(context?.Source, context?.FieldAst?.Name);

        private static object Resolve(object source, string name)
        {
            if (source == null || name == null)
                return null;

            // We use reflection to create a delegate to access the property
            // Then cache the delegate
            // This is over 10x faster that just using reflection to get the property value
            var func = _propertyDelegates.GetOrAdd((source.GetType(), name), t => CreatePropertyDelegate(t.targetType, t.propertyName));
            return func(source);
        }

        /// <summary>
        /// Dynamically creates the necessary delegate in runtime to get the property value of the specified type.
        ///
        /// Example:
        /// public class Person
        /// {
        ///     public int Age { get; set; }
        /// }
        ///
        /// So resulting Func will be generated as {x => Convert(Convert(x, Person).Age, Object)}
        /// 1. First, the input parameter 'x' is converted from the object to a specific type.
        /// 2. The required property is extracted from casted value.
        /// 3. Then result is converted again to the object and returned from the method.
        /// </summary>
        /// <param name="target"> The type from which you want to get the property. </param>
        /// <param name="name"> Property name. </param>
        /// <returns> Compiled delegate to get property value. </returns>
        private static Func<object, object> CreatePropertyDelegate(Type target, string name)
        {
            var parameter = Expression.Parameter(typeof(object), "x");
            // Property name resolution works fine even for 'age' name instead of 'Age' since Expression.Property uses
            // BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy under the hood.
            // If the property is not found then will throw ArgumentException: Instance property 'name' is not defined for type 'target' (Parameter 'propertyName')
            var member = Expression.Property(Expression.Convert(parameter, target), name); 
            var lambda = Expression.Lambda<Func<object, object>>(Expression.Convert(member, typeof(object)), parameter);
            return lambda.Compile();
        }
    }
}
