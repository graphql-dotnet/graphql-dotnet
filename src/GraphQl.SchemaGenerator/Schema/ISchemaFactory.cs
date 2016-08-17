using GraphQL.Types;

namespace GraphQL.SchemaGenerator.Schema
{
    public interface ISchemaFactory
    {
        ISchema GetOrCreateSchema();
    }

}
