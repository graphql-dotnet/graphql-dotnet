using GraphQL.Federation.Attributes;

namespace GraphQL.Federation.TypeFirst.Sample4.Schema;

[Key("id")]
public class User
{
    [Id]
    public int Id { get; set; }
    public required string Username { get; set; }

    // valid:
    //[FederationResolver]
    //public Task<User?> GetUserByIdAsync([FromServices] Data data)
    //{
    //    return data.GetUserByIdAsync(Id);
    //}

    // also valid:
    [FederationResolver]
    public static Task<User?> GetUserByIdAsync([Id] int id, [FromServices] Data data)
    {
        return data.GetUserByIdAsync(id);
    }
}
