using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL
{
    public interface IGraphQLRequestReader
    {
        ValueTask<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken);
        T Read<T>(string json);
    }
}
