using System;
using System.Collections.Generic;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/issues/661
    public class Issue661
    {
        [Fact]
        public void Cache_Injection_Should_Work()
        {
            var services = new ServiceCollection();

            services.AddDistributedMemoryCache();
            services.AddSingleton<ISchema, Issue661Schema>();
            services.AddSingleton<Issue661Query>();
            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddSingleton<IDocumentWriter, DocumentWriter>();

            using var provider = services.BuildServiceProvider();
            var cache = provider.GetRequiredService<IDistributedCache>();
            cache.GetString("mykey").ShouldBeNull();
            cache.SetString("mykey", "myvalue");
            cache.GetString("mykey").ShouldBe("myvalue");

            var executer = provider.GetRequiredService<IDocumentExecuter>();
            var result = executer.ExecuteAsync(options =>
            {
                options.Schema = provider.GetRequiredService<ISchema>();
                options.Query = "{ get_cached }";
            }).GetAwaiter().GetResult();

            result.Errors.ShouldBeNull();
            var data = (Dictionary<string, object>)result.Data;
            data.Count.ShouldBe(1);
            data["get_cached"].ShouldBe("myvalue");
        }
    }

    public class Issue661Schema : Schema
    {
        public Issue661Schema(IServiceProvider provider, IDistributedCache cache) : base(provider)
        {
            Query = new Issue661Query(cache);
        }
    }

    public class Issue661Query : ObjectGraphType
    {
        private readonly IDistributedCache _cache;

        public Issue661Query(IDistributedCache cache)
        {
            _cache = cache;

            Field<StringGraphType>(
                "get_cached",
                resolve: ctx =>
                {
                    var value = _cache.GetString("mykey");
                    value.ShouldBe("myvalue");
                    return value;
                });
        }
    }
}
