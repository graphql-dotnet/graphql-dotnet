using GraphQL.Types;

namespace GraphQL.Instrumentation
{
    public static class SchemaExtensions
    {
        public static void Instrument(this ISchema schema, Timings timings)
        {
            schema.AllTypes.Apply(item =>
            {
                var complex = item as IComplexGraphType;
                complex?.Fields.Apply(field =>
                {
                    field.Resolver = new InstrumentedResolver(field.Resolver, timings);
                });
            });
        }
    }
}
