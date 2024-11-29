namespace DataLoaderGql.Types;

public class Car
{
    public static Car Create(string model, int year = 2024, int price = 25_000) => new()
    {
        Model = model,
        ModelYear = year,
        Price = price
    };
    public int Id { get; set; }
    public required string Model { get; set; }
    public int ModelYear { get; set; }
    public int Price { get; set; }
    public int SalesPersonId { get; set; }
}
