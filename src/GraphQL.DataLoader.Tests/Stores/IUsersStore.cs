using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;

namespace GraphQL.DataLoader.Tests.Stores
{
    public interface IUsersStore
    {
        Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken);

        Task<IDictionary<int, User>> GetUsersByIdAsync(IEnumerable<int> userIds, CancellationToken cancellationToken);
    }
}
