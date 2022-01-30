using System;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    /// <summary>
    /// Allows additional configuration to be applied to a type or field definition.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public abstract class GraphQLAttribute : Attribute
    {
        /// <summary>
        /// Updates the properties of the specified <see cref="TypeConfig"/> as necessary.
        /// </summary>
        public virtual void Modify(TypeConfig type)
        {
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="FieldConfig"/> as necessary.
        /// </summary>
        public virtual void Modify(FieldConfig field)
        {
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="IGraphType"/> as necessary.
        /// </summary>
        public virtual void Modify(IGraphType graphType)
        {
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="FieldType"/> as necessary.
        /// </summary>
        /// <param name="fieldType">The <see cref="FieldType"/> to update.</param>
        /// <param name="isInputType">Indicates if the graph type containing this field is an input type.</param>
        public virtual void Modify(FieldType fieldType, bool isInputType)
        {
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="TypeInformation"/> as necessary.
        /// </summary>
        public virtual void Modify(TypeInformation typeInformation)
        {
        }
    }
}
