namespace GraphQL.Federation.Sample2.Schema;

public class Product : IHasId
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category()
        => new Category { Id = CategoryId };
}
