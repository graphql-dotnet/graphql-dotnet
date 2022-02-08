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
        public virtual void VisitSchema(ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitDirective(Directive directive, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitScalar(ScalarGraphType type, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitObject(IObjectGraphType type, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitInputObject(IInputObjectGraphType type, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitInterfaceFieldDefinition(FieldType field, IInterfaceGraphType type, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitDirectiveArgumentDefinition(QueryArgument argument, Directive directive, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitInterface(IInterfaceGraphType type, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitUnion(UnionGraphType type, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitEnum(EnumerationGraphType type, ISchema schema)
        {
        }

        /// <inheritdoc />
        public virtual void VisitEnumValue(EnumValueDefinition value, EnumerationGraphType type, ISchema schema)
        {
        }
    }
}
