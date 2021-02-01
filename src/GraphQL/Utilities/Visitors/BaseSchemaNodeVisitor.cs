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
        public virtual void VisitDirective(DirectiveGraphType directive, Schema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitScalar(ScalarGraphType scalar, Schema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitObject(IObjectGraphType type, Schema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitInputObject(IInputObjectGraphType type, Schema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitFieldDefinition(FieldType field, Schema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitInputFieldDefinition(FieldType field, Schema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitFieldArgumentDefinition(QueryArgument argument, Schema schema)
        {
        }
        /// <inheritdoc />
        public virtual void VisitDirectiveArgumentDefinition(QueryArgument argument, Schema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitInterface(IInterfaceGraphType iface, Schema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitUnion(UnionGraphType union, Schema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitEnum(EnumerationGraphType type, Schema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitEnumValue(EnumValueDefinition value, Schema schema)
        {
        }
    }
}
