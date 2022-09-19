using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__schema</c> meta-field is available on the root of a query operation and
    /// returns a <see cref="__Schema"/> graph type for the schema.
    /// </summary>
    public class SchemaMetaFieldType : FieldType
    {
        /// <summary>
        /// Initializes a new instance of the <c>__schema</c> meta-field.
        /// </summary>
        public SchemaMetaFieldType()
        {
            SetName("__schema", validate: false);
            Type = typeof(__Schema);
            Description = "Access the current type schema of this server.";
            Resolver = new FuncFieldResolver<ISchema>(context => context.Schema);
        }
    }
}
