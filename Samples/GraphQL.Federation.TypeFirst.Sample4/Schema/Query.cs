namespace GraphQL.Federation.TypeFirst.Sample4.Schema;

public static class Query
{
    public static Task<IEnumerable<User>> Users([FromServices] Data data)
        => data.GetUsersAsync();
}
