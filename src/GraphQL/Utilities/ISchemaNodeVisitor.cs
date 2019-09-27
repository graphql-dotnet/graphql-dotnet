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
        protected SyncToAsyncResolverAdapter WrapResolver(IFieldResolver resolver)
        {
            var inner = resolver ?? new NameFieldResolver();
            return new SyncToAsyncResolverAdapter(inner);
        }

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
