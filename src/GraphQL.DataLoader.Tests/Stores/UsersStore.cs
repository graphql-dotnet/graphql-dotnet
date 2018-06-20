using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.DataLoader.Tests.Models;

namespace GraphQL.DataLoader.Tests.Stores
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

        private IEnumerable<int> _getUsersById_UserIds;
        public IEnumerable<int> GetUsersById_UserIds => _getUsersById_UserIds;

        private int _getAllUsersCalled;
        public int GetAllUsersCalledCount => _getAllUsersCalled;

        public async Task<Dictionary<int, User>> GetUsersByIdAsync(IEnumerable<int> userIds,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Interlocked.Increment(ref _getUsersByIdCalled);
            Interlocked.Exchange(ref _getUsersById_UserIds, userIds);

            await Task.Yield();

            cancellationToken.ThrowIfCancellationRequested();

            return _users
                .Join(userIds, u => u.UserId, x => x, (u, _) => u)
                .ToDictionary(u => u.UserId);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(
            CancellationToken cancellationToken = default(CancellationToken),
            int delayMs = 1)
        {
            Interlocked.Increment(ref _getAllUsersCalled);

            await Task.Delay(delayMs);

            cancellationToken.ThrowIfCancellationRequested();

            return _users.AsReadOnly();
        }

        public async Task<IEnumerable<User>> GetDuplicateUsersAsync(IEnumerable<int> userIds, CancellationToken cancellationToken)
        {
            await Task.Yield();

            cancellationToken.ThrowIfCancellationRequested();

            // Return 2 users for each key
            return userIds.SelectMany(x =>
                Enumerable.Repeat(_users.Find(u => u.UserId == x), 2)
            ).ToList();
        }

        public Task<IEnumerable<User>> ThrowExceptionImmediatelyAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new Exception("Immediate");
        }

        public async Task<IEnumerable<User>> ThrowExceptionDeferredAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await Task.Yield();

            throw new Exception("Deferred");
        }
    }
}
