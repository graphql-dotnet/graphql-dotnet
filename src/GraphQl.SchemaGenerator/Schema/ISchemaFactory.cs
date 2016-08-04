using GraphQL.Types;

namespace GraphQl.SchemaGenerator.Schema
{
    public interface ISchemaFactory
    {
        ISchema GetOrCreateSchema();
    }

}
