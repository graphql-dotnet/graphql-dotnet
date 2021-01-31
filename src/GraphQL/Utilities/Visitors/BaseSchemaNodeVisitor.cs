using GraphQL.Types;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Base class implementing <see cref="ISchemaNodeVisitor"/>. Does nothing.
    /// Inherit from it if you need to override only some of the methods.
    /// </summary>
    public abstract class BaseSchemaNodeVisitor : ISchemaNodeVisitor
    {
        /// <inheritdoc />
        public virtual void VisitSchema(Schema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitDirective(DirectiveGraphType directive)
        {
        }

        /// <inheritdoc />
        public virtual void VisitScalar(ScalarGraphType scalar)
        {
        }

        /// <inheritdoc />
        public virtual void VisitObject(IObjectGraphType type)
        {
        }

        /// <inheritdoc />
        public virtual void VisitInputObject(IInputObjectGraphType type)
        {
        }

        /// <inheritdoc />
        public virtual void VisitFieldDefinition(FieldType field)
        {
        }

        /// <inheritdoc />
        public virtual void VisitInputFieldDefinition(FieldType field)
        {
        }

        /// <inheritdoc />
        public virtual void VisitFieldArgumentDefinition(QueryArgument argument)
        {
        }
        /// <inheritdoc />
        public virtual void VisitDirectiveArgumentDefinition(QueryArgument argument)
        {
        }

        /// <inheritdoc />
        public virtual void VisitInterface(IInterfaceGraphType iface)
        {
        }

        /// <inheritdoc />
        public virtual void VisitUnion(UnionGraphType union)
        {
        }

        /// <inheritdoc />
        public virtual void VisitEnum(EnumerationGraphType type)
        {
        }

        /// <inheritdoc />
        public virtual void VisitEnumValue(EnumValueDefinition value)
        {
        }
    }
}
