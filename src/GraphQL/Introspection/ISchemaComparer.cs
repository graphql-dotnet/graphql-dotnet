using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// Provides the ability to order the schema elements upon introspection.
    /// </summary>
    public interface ISchemaComparer
    {
        /// <summary>
        /// Returns a comparer for GraphQL types.
        /// </summary>
        IComparer<IGraphType> TypeComparer { get; }

        /// <summary>
        /// Returns a comparer for fields withing enclosing type.
        /// If this returns null then the original field ordering is preserved.
        /// </summary>
        /// <param name="parent"> Parent type to which the fields belong. </param>
        IComparer<IFieldType> FieldComparer(IGraphType parent);

        /// <summary>
        /// Returns a comparer for field arguments.
        /// </summary>
        /// <param name="field"> The field to which the arguments belong. </param>
        IComparer<QueryArgument> ArgumentComparer(IFieldType field);

        /// <summary>
        /// Returns a comparer for enum values.
        /// </summary>
        /// <param name="parent"> The enumeration to which the enum values belong. </param>
        IComparer<EnumValueDefinition> EnumValueComparer(EnumerationGraphType parent);

        /// <summary>
        /// Returns a comparer for GraphQL directives.
        /// </summary>
        IComparer<DirectiveGraphType> DirectiveComparer { get; }
    }

    /// <summary>
    /// Default schema comparer. By default only fields are ordered by their names within enclosing type.
    /// </summary>
    public class DefaultSchemaComparer : ISchemaComparer
    {
        private static readonly FieldByNameComparer _instance = new FieldByNameComparer();

        private sealed class FieldByNameComparer : IComparer<IFieldType>
        {
            public int Compare(IFieldType x, IFieldType y) => x.Name.CompareTo(y.Name);
        }

        /// <inheritdoc/>
        public virtual IComparer<IGraphType> TypeComparer => null;

        /// <inheritdoc/>
        public virtual IComparer<DirectiveGraphType> DirectiveComparer => null;

        /// <inheritdoc/>
        public virtual IComparer<QueryArgument> ArgumentComparer(IFieldType field) => null;

        /// <inheritdoc/>
        public virtual IComparer<EnumValueDefinition> EnumValueComparer(EnumerationGraphType parent) => null;

        /// <inheritdoc/>
        public virtual IComparer<IFieldType> FieldComparer(IGraphType parent) => _instance;
    }
}
