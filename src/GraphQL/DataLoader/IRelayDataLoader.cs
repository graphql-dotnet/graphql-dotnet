using System.Threading.Tasks;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.DataLoader
{
    public class PageArguments
    {
        public string After { get; set; }
        public string Before { get; set; }
        public int First { get; set; }
        public int Last { get; set; }
    }

    public class PageRequest<TKey>
    {
        public TKey Key { get; set; }
        public PageArguments PageArguments { get; set; }
    }

    public interface IRelayDataLoader<TKey, T>
    {
        Task<Connection<T>> LoadPageAsync(PageRequest<TKey> pagedKey);
    }
}
