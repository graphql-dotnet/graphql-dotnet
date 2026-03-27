namespace GraphQL.SchemaFirst.Sample.Schema;

public class Droid
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? PrimaryFunction { get; set; }
}
