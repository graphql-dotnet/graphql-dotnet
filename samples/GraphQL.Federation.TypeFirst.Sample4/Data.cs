using GraphQL.Federation.TypeFirst.Sample4.Schema;

namespace GraphQL.Federation.TypeFirst.Sample4;

public class Data
{
    private readonly List<User> _users = new() {
        new User { Id = 1, Username = "Username 1" },
        new User { Id = 2, Username = "Username 2" },
        new User { Id = 3, Username = "Username 3" },
    };

    public Task<IEnumerable<User>> GetUsersAsync()
    {
        return Task.FromResult(_users.AsEnumerable());
    }

    public Task<User?> GetUserByIdAsync(int id)
    {
        return Task.FromResult(_users.SingleOrDefault(x => x.Id == id));
    }
}
