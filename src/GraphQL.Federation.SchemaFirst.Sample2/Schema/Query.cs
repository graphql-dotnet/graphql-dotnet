namespace GraphQL.Federation.SchemaFirst.Sample2.Schema;

public class Query
{
    public Task<IEnumerable<Product>> Products([FromServices] Data data)
        => data.GetProductsAsync();
}
