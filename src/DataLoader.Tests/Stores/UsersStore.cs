using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataLoader.Tests.Models;

namespace DataLoader.Tests.Stores
{
    public class UsersStore
    {
        private readonly List<User> _users = new List<User>();

        public void AddUsers(params User[] users)
        {
            _users.AddRange(users);
        }

        private int _getUsersByIdCalled;
        public int GetUsersByIdCalledCount => _getUsersByIdCalled;

        private int _getAllUsersCalled;
        public int GetAllUsersCalledCount => _getAllUsersCalled;

        public async Task<Dictionary<int, User>> GetUsersByIdAsync(IEnumerable<int> userIds)
        {
            Interlocked.Increment(ref _getUsersByIdCalled);

            await Task.Delay(1);

            return _users
                .Join(userIds, u => u.UserId, x => x, (u, _) => u)
                .ToDictionary(u => u.UserId);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            Interlocked.Increment(ref _getAllUsersCalled);

            await Task.Delay(1);

            return _users.AsReadOnly();
        }
    }
}
