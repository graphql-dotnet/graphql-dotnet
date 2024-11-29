namespace GraphQL.Federation.TypeFirst.Sample4.Schema;

public class Query
{
    public static Task<IEnumerable<User>> Users([FromServices] Data data)
        => data.GetUsersAsync();
}
