using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    public interface ISchemaNodeVisitor
    {
        void VisitSchema(Schema schema);
        void VisitScalar(ScalarGraphType scalar);
        void VisitObject(ObjectGraphType type);
        void VisitObject(IObjectGraphType type);
        void VisitField(FieldType field);
        void VisitArgument(QueryArgument argument);
        void VisitInterface(InterfaceGraphType interfaceDefinition);
        void VisitUnion(UnionGraphType union);
        void VisitEnumeration(EnumerationGraphType type);
        void VisitEnumerationValue(EnumValueDefinition value);
        void VisitInputObject(InputObjectGraphType type);
        void VisitInputField(FieldType type);
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

        public virtual void VisitObject(ObjectGraphType type)
        {
        }

        public virtual void VisitObject(IObjectGraphType type)
        {
        }

        public virtual void VisitField(FieldType field)
        {
        }

        public virtual void VisitArgument(QueryArgument argument)
        {
        }

        public virtual void VisitInterface(InterfaceGraphType interfaceDefinition)
        {
        }

        public virtual void VisitUnion(UnionGraphType union)
        {
        }

        public virtual void VisitEnumeration(EnumerationGraphType type)
        {
        }

        public virtual void VisitEnumerationValue(EnumValueDefinition value)
        {
        }

        public virtual void VisitInputObject(InputObjectGraphType type)
        {
        }

        public virtual void VisitInputField(FieldType value)
        {
        }
    }
}
