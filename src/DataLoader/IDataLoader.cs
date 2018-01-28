using System.Threading;
using System.Threading.Tasks;

namespace DataLoader
{
    public interface IDataLoader
    {
        void Dispatch(CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IDataLoader<T>
    {
        Task<T> LoadAsync();
    }

    public interface IDataLoader<TKey, T>
    {
        Task<T> LoadAsync(TKey key);
    }
}
