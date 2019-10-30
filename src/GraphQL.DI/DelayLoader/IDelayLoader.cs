using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.DI.DelayLoader
{
    public interface IDelayLoader
    {
        Task LoadAsync();
        Task LoadAsync(CancellationToken cancellationToken);
    }
}
