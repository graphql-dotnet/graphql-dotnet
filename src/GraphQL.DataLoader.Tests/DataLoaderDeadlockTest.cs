using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GraphQL.DataLoader.Tests
{
    public class DataLoaderDeadlockTest : QueryTestBase
    {
        protected override void ConfigureServices(ServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddSingleton<DataLoaderSchema>();
        }
        [Fact]
        public void Await_before_LoadAsync_should_not_deadlock()
        {
            // Tests Github issue: DataLoader deadlock with multiple awaits #945
            AssertQuerySuccess<DataLoaderSchema>(
                query: "{ true }",
                expected: @"
                { true: true }
                ");
        }
    }
    public class DataLoaderSchema : Schema
    {
        public DataLoaderSchema(IServiceProvider services, IDataLoaderContextAccessor accessor)
            :base(services)
        {
            var query = new ObjectGraphType();
            query.Field<BooleanGraphType, bool>()
                .Name("True")
                .ResolveAsync(async ctx =>
                {
                    await Task.Delay(1);

                    var loader = accessor.Context.GetOrAddLoader("GetTrue",
                        () => Task.FromResult(true));

                    return await loader.LoadAsync();
                });

            Query = query;
        }
    }
}
