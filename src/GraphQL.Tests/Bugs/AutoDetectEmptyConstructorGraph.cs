using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class AutoDetectEmptyConstructorGraph
    {
        [Fact]
        public async Task Simple()
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<ISchema, AutoDetectEmptyConstructorSchema>();
            var provider = collection.BuildServiceProvider();
            var options = new ExecutionOptions
            {
                Schema = provider.GetService<ISchema>(),
                Query = "{ grumpy }",
                ThrowOnUnhandledException = true
            };

            var executer = new DocumentExecuter();

            var result = await executer.ExecuteAsync(options);
            var data = (Dictionary<string, object>)result.Data;
            Assert.Equal("GRUMPY", data["grumpy"]);
        }
    }

    public class AutoDetectEmptyConstructorSchema : Schema
    {
        public AutoDetectEmptyConstructorSchema(IServiceProvider services)
            : base(services)
        {
            Query = new AutoDetectEmptyConstructorQuery();
        }
    }

    public class AutoDetectEmptyConstructorQuery : ObjectGraphType
    {
        public AutoDetectEmptyConstructorQuery()
        {
            Field<EnumerationGraphType<MyEnum>>(
                "grumpy",
                resolve: ctx => MyEnum.Grumpy);
        }
    }

    public enum MyEnum
    {
        Grumpy = 0,
        Happy = 1,
        Sleepy = 2,
    }
}
