namespace GraphQL.Federation.SchemaFirst.Sample2.Schema;

public class Product : IHasId
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int CategoryId { get; set; }

    // categories do not actually exist in this sample
    // but we need to define the relationship for GraphQL Federation
    // so it can be used by the router to be retrieved from the
    // Sample1 project
    public Category Category()
        => new Category { Id = CategoryId };
}
