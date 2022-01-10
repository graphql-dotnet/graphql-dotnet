using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Serialization.SystemTextJson
{
    public class ExtensionTests
    {
        [Fact]
        public async Task ExecuteAsync_Honors_CancellationToken()
        {
            var schema = new Schema();
            var query = new ObjectGraphType();
            query.Field<StringGraphType>("hero", resolve: context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                return null;
            });
            schema.Query = query;
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Should.ThrowAsync<OperationCanceledException>(() => schema.ExecuteAsync(c => c.Query = "{hero}", cts.Token));
        }
    }
}
