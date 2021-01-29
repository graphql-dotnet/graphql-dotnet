using GraphQL.Types;

namespace GraphQL.Utilities
{
    // interface members aligned according to  https://www.apollographql.com/docs/graphql-tools/schema-directives/#implementing-schema-directives
    public interface ISchemaNodeVisitor
    {
        void VisitSchema(Schema schema);

        void VisitDirective(DirectiveGraphType directive);

        void VisitScalar(ScalarGraphType scalar);

        void VisitObject(IObjectGraphType type);

        void VisitFieldDefinition(FieldType field);

        void VisitFieldArgumentDefinition(QueryArgument argument);

        void VisitDirectiveArgumentDefinition(QueryArgument argument);

        void VisitInterface(IInterfaceGraphType iface);

        void VisitUnion(UnionGraphType union);

        void VisitEnum(EnumerationGraphType type);

        void VisitEnumValue(EnumValueDefinition value);

        void VisitInputObject(IInputObjectGraphType type);

        void VisitInputFieldDefinition(FieldType field);
    }

    public abstract class BaseSchemaNodeVisitor : ISchemaNodeVisitor
    {
        public virtual void VisitSchema(Schema schema)
        {
        }

        public virtual void VisitDirective(DirectiveGraphType directive)
        {
        }

        public virtual void VisitScalar(ScalarGraphType scalar)
        {
        }

        public virtual void VisitObject(IObjectGraphType type)
        {
        }

        public virtual void VisitFieldDefinition(FieldType field)
        {
        }

        public virtual void VisitFieldArgumentDefinition(QueryArgument argument)
        {
        }

        public virtual void VisitDirectiveArgumentDefinition(QueryArgument argument)
        {
        }

        public virtual void VisitInterface(IInterfaceGraphType iface)
        {
        }

        public virtual void VisitUnion(UnionGraphType union)
        {
        }

        public virtual void VisitEnum(EnumerationGraphType type)
        {
        }

        public virtual void VisitEnumValue(EnumValueDefinition value)
        {
        }

        public virtual void VisitInputObject(IInputObjectGraphType type)
        {
        }

        public virtual void VisitInputFieldDefinition(FieldType field)
        {
        }
    }
}
