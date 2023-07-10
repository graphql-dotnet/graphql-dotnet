namespace GraphQL.Federation.Sample2.Schema;

public class Category : IHasId
{
    public int Id { get; set; }
    public Task<IEnumerable<Product>> Products([FromServices] Data data)
        => data.GetProductsByCategoryIdAsync(Id);
}
