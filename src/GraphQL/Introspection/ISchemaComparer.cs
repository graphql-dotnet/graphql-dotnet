using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// Provides the ability to order the schema elements upon introspection or printing.
    /// </summary>
    public interface ISchemaComparer
    {
        /// <summary>
        /// Returns a comparer for GraphQL types.
        /// If this returns <see langword="null"/> then the original type ordering is preserved.
        /// </summary>
        IComparer<IGraphType>? TypeComparer { get; }

        /// <summary>
        /// Returns a comparer for fields withing enclosing type.
        /// If this returns <see langword="null"/> then the original field ordering is preserved.
        /// </summary>
        /// <param name="parent"> Parent type to which the fields belong. </param>
        IComparer<IFieldType>? FieldComparer(IGraphType parent);

        /// <summary>
        /// Returns a comparer for field arguments.
        /// If this returns <see langword="null"/> then the original argument ordering is preserved.
        /// </summary>
        /// <param name="field"> The field to which the arguments belong. </param>
        IComparer<QueryArgument>? ArgumentComparer(IFieldType field);

        /// <summary>
        /// Returns a comparer for enum values.
        /// If this returns <see langword="null"/> then the original enum value ordering is preserved.
        /// </summary>
        /// <param name="parent"> The enumeration to which the enum values belong. </param>
        IComparer<EnumValueDefinition>? EnumValueComparer(EnumerationGraphType parent);

        /// <summary>
        /// Returns a comparer for GraphQL directives.
        /// If this returns <see langword="null"/> then the original directive ordering is preserved.
        /// </summary>
        IComparer<Directive>? DirectiveComparer { get; }
    }

    /// <summary>
    /// Default schema comparer. By default all elements are returned as is, no sorting is applied.
    /// </summary>
    public class DefaultSchemaComparer : ISchemaComparer
    {
        /// <inheritdoc/>
        public virtual IComparer<IGraphType>? TypeComparer => null;

        /// <inheritdoc/>
        public virtual IComparer<Directive>? DirectiveComparer => null;

        /// <inheritdoc/>
        public virtual IComparer<QueryArgument>? ArgumentComparer(IFieldType field) => null;

        /// <inheritdoc/>
        public virtual IComparer<EnumValueDefinition>? EnumValueComparer(EnumerationGraphType parent) => null;

        /// <inheritdoc/>
        public virtual IComparer<IFieldType>? FieldComparer(IGraphType parent) => null;
    }

    /// <summary>
    /// All elements are sorted in alphabetical order of their names.
    /// </summary>
    public class AlphabeticalSchemaComparer : ISchemaComparer
    {
        private static readonly TypeByNameComparer _instance1 = new();
        private static readonly DirectiveByNameComparer _instance2 = new();
        private static readonly ArgumentByNameComparer _instance3 = new();
        private static readonly EnumValueByNameComparer _instance4 = new();
        private static readonly FieldByNameComparer _instance5 = new();

        private sealed class TypeByNameComparer : IComparer<IGraphType>
        {
            public int Compare(IGraphType? x, IGraphType? y) => (x?.Name ?? "").CompareTo(y?.Name ?? "");
        }

        private sealed class DirectiveByNameComparer : IComparer<Directive>
        {
            public int Compare(Directive? x, Directive? y) => (x?.Name ?? "").CompareTo(y?.Name ?? "");
        }

        private sealed class ArgumentByNameComparer : IComparer<QueryArgument>
        {
            public int Compare(QueryArgument? x, QueryArgument? y) => (x?.Name ?? "").CompareTo(y?.Name ?? "");
        }

        private sealed class EnumValueByNameComparer : IComparer<EnumValueDefinition>
        {
            public int Compare(EnumValueDefinition? x, EnumValueDefinition? y) => (x?.Name ?? "").CompareTo(y?.Name ?? "");
        }

        private sealed class FieldByNameComparer : IComparer<IFieldType>
        {
            public int Compare(IFieldType? x, IFieldType? y) => (x?.Name ?? "").CompareTo(y?.Name ?? "");
        }

        /// <inheritdoc/>
        public virtual IComparer<IGraphType> TypeComparer => _instance1;

        /// <inheritdoc/>
        public virtual IComparer<Directive> DirectiveComparer => _instance2;

        /// <inheritdoc/>
        public virtual IComparer<QueryArgument> ArgumentComparer(IFieldType field) => _instance3;

        /// <inheritdoc/>
        public virtual IComparer<EnumValueDefinition> EnumValueComparer(EnumerationGraphType parent) => _instance4;

        /// <inheritdoc/>
        public virtual IComparer<IFieldType> FieldComparer(IGraphType parent) => _instance5;
    }
}
