namespace DataLoaderGql.Types;

public class Salesperson
{
    public int Id { get; set; } 
    public string Name { get; set; } 
    public List<Car> AssignedCars { get; set; }
}