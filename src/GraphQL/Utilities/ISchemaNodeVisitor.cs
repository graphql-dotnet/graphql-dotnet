using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public interface ISchemaNodeVisitor
    {
        void VisitSchema(Schema schema);
        void VisitScalar(ScalarGraphType scalar);
        void VisitObjectGraphType(ObjectGraphType type);
        void VisitObjectGraphType(IObjectGraphType type);
        void VisitField(FieldType field);
        void VisitArgumentDefinition(QueryArgument argument);
        void VisitInterface(InterfaceGraphType interfaceDefinition);
        void VisitUnion(UnionGraphType union);
        void VisitEnum(EnumerationGraphType type);
        void VisitEnumValue(EnumValueDefinition value);
        void VisitInputObject(InputObjectGraphType type);
        void VisitInputFieldDefinition(FieldType type);
    }

    public abstract class BaseSchemaNodeVisitor : ISchemaNodeVisitor
    {
        protected SyncToAsyncResolverAdapter WrapResolver(IFieldResolver resolver)
        {
            var inner = resolver ?? new NameFieldResolver();
            return new SyncToAsyncResolverAdapter(inner);
        }

        public virtual void VisitSchema(Schema schema)
        {
        }

        public virtual void VisitScalar(ScalarGraphType scalar)
        {
        }

        public virtual void VisitObjectGraphType(ObjectGraphType type)
        {
        }

        public virtual void VisitObjectGraphType(IObjectGraphType type)
        {
        }

        public virtual void VisitField(FieldType field)
        {
        }

        public virtual void VisitArgumentDefinition(QueryArgument argument)
        {
        }

        public virtual void VisitInterface(InterfaceGraphType interfaceDefinition)
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

        public virtual void VisitInputFieldDefinition(FieldType value)
        {
        }
    }
}
