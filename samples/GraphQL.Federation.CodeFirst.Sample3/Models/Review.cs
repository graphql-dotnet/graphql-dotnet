namespace GraphQL.Federation.CodeFirst.Sample3.Models;

public class Review
{
    public required int Id { get; set; }
    public required int ProductId { get; set; }
    public required int UserId { get; set; }
    public required string Content { get; set; }
}
