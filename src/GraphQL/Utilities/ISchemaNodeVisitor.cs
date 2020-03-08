using GraphQL.Types;

namespace GraphQL.Utilities
{
    // interface members aligned according to  https://www.apollographql.com/docs/graphql-tools/schema-directives/#implementing-schema-directives
    public interface ISchemaNodeVisitor
    {
        void VisitSchema(Schema schema);

        void VisitScalar(ScalarGraphType scalar);

        void VisitObject(IObjectGraphType type);

        void VisitFieldDefinition(FieldType field);

        void VisitArgumentDefinition(QueryArgument argument);

        void VisitInterface(InterfaceGraphType iface);

        void VisitUnion(UnionGraphType union);

        void VisitEnum(EnumerationGraphType type);

        void VisitEnumValue(EnumValueDefinition value);

        void VisitInputObject(InputObjectGraphType type);

        void VisitInputFieldDefinition(FieldType field);
    }

    public abstract class BaseSchemaNodeVisitor : ISchemaNodeVisitor
    {
        public virtual void VisitSchema(Schema schema)
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

        public virtual void VisitArgumentDefinition(QueryArgument argument)
        {
        }

        public virtual void VisitInterface(InterfaceGraphType iface)
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

        public virtual void VisitInputObject(InputObjectGraphType type)
        {
        }

        public virtual void VisitInputFieldDefinition(FieldType field)
        {
        }
    }
}
