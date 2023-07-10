namespace GraphQL.Federation.Sample1.Schema;

public class Category : IHasId
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}
