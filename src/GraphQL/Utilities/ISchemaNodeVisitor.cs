using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public interface ISchemaNodeVisitor
    {
        void VisitObjectGraphType(ObjectGraphType type);
        void VisitField(FieldType field);
        void VisitEnumValue(EnumValueDefinition value);
    }

    public abstract class BaseSchemaNodeVisitor : ISchemaNodeVisitor
    {
        public virtual void VisitObjectGraphType(ObjectGraphType type)
        {
        }

        public virtual void VisitField(FieldType field)
        {
        }

        public virtual void VisitEnumValue(EnumValueDefinition value)
        {
        }
    }
}
