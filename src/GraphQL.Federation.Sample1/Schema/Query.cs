namespace GraphQL.Federation.Sample1.Schema;

public class Query
{
    public Task<IEnumerable<Category>> Categories([FromServices] Data data)
        => data.GetCategoriesAsync();
}
