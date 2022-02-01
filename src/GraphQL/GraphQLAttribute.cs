using System;
using System.Reflection;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    /// <summary>
    /// Allows additional configuration to be applied to a type, field or argument definition.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
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

        /// <summary>
        /// Updates the properties of the specified <see cref="ArgumentInformation{TReturnType}"/> as necessary.
        /// </summary>
        public virtual void Modify<TReturnType>(ArgumentInformation<TReturnType> argumentInformation)
        {
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="QueryArgument"/> as necessary.
        /// </summary>
        public virtual void Modify(QueryArgument queryArgument)
        {
        }

        /// <summary>
        /// Determines if a specified member should be included during automatic generation
        /// of a graph type from a CLR type.
        /// </summary>
        public virtual bool ShouldInclude(MemberInfo memberInfo, bool isInputType) => true;
    }
}
