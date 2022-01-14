using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// <para>
    /// Attempts to return a value for a field from the graph's source object, matching the name of
    /// the field to a property or a method with the same name on the source object.
    /// </para><para>
    /// Call <see cref="Instance"/> to retrieve an instance of this class.
    /// </para>
    /// </summary>
    public class NameFieldResolver : IFieldResolver
    {
        private static readonly ConcurrentDictionary<(Type targetType, string name), Func<object, object>> _delegates
            = new ConcurrentDictionary<(Type, string), Func<object, object>>();

        private NameFieldResolver() { }

        /// <summary>
        /// Returns the static instance of the <see cref="NameFieldResolver"/> class.
        /// </summary>
        public static NameFieldResolver Instance { get; } = new NameFieldResolver();

        /// <inheritdoc/>
        public object? Resolve(IResolveFieldContext context) => Resolve(context?.Source, (string)context?.FieldAst?.Name); //TODO:!!!!alloc

        private static object? Resolve(object? source, string? name)
        {
            if (source == null || name == null)
                return null;

            // We use reflection to create a delegate to access the property/method
            // Then cache the delegate
            // This is over 10x faster that just using reflection to get the property/method value
            var func = _delegates.GetOrAdd((source.GetType(), name), t => CreateDelegate(t.targetType, t.name));
            return func(source);
        }

        /// <summary>
        /// <para>
        /// Dynamically creates the necessary delegate in runtime to get the property/method value of the specified type.
        /// </para><para>
        /// Example:
        /// </para><code>
        /// public class Person
        /// {
        ///     public int Age { get; set; }
        /// }
        /// </code>
        /// <para>
        /// So resulting Func will be generated as <c>{x => Convert(Convert(x, Person).Age, Object)}</c><br/>
        /// 1. First, the input parameter 'x' is converted from the object to a specific type.<br/>
        /// 2. The required property or method is extracted from casted value.<br/>
        /// 3. Then result is converted again to the object and returned from the method.
        /// </para>
        /// </summary>
        /// <param name="target"> The type from which you want to get the value. </param>
        /// <param name="name"> Property/method name. </param>
        /// <returns> Compiled delegate to get the value. </returns>
        private static Func<object, object> CreateDelegate(Type target, string name)
        {
            var parameter = Expression.Parameter(typeof(object), "x");

            var member = Expression.Convert(parameter, target);
            var lambda = Expression.Lambda<Func<object, object>>(Expression.Convert(CreateAccessorExpression(), typeof(object)), parameter);
            return lambda.Compile();

            Expression CreateAccessorExpression()
            {
                try
                {
                    try
                    {
                        // Property name resolution works fine even for 'age' name instead of 'Age' since Expression.Property uses
                        // BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy under the hood.
                        // If the property is not found then will throw ArgumentException: Instance property 'name' is not defined for type 'target' (Parameter 'propertyName')
                        return Expression.Property(member, name);
                    }
                    catch (ArgumentException)
                    {
                        // Method name resolution works fine even for 'get' name instead of 'Get' since Expression.Call uses
                        // Type.FilterNameIgnoreCase under the hood.
                        // If the method is not found then will throw InvalidOperationException: 'No method 'name' on type 'target' is compatible with the supplied arguments.
                        return Expression.Call(member, name, Type.EmptyTypes);
                    }
                }
                catch (InvalidOperationException)
                {
                    throw new InvalidOperationException($"Expected to find property or method '{name}' on type '{target.Name}' but it does not exist.");
                }
            }
        }
    }
}
