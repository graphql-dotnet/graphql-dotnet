using System;
using GraphQL.Conversion;

namespace GraphQL
{
    /// <summary>
    /// Marks a class (graph type) or property (field) with a specified GraphQL name.
    /// Note that the specified name will be translated by the schema's <see cref="INameConverter"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class NameAttribute : GraphQLAttribute
    {
        /// <inheritdoc cref="NameAttribute"/>
        public NameAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Returns the GraphQL name of the class (graph type) or property (field).
        /// </summary>
        public string Name { get; }
        
    }
}
