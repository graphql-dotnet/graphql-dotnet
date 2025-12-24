using GraphQL.DataLoader.Tests.Models;

namespace GraphQL.DataLoader.Tests.Stores;

public interface IUsersStore
{
    public Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken);

    public Task<IDictionary<int, User>> GetUsersByIdAsync(IEnumerable<int> userIds, CancellationToken cancellationToken);
}
