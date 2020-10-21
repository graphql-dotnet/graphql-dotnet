using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class AutoDetectEnumGraph
    {
        [Fact]
        public async Task Simple_Enum()
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<ISchema, AutoDetectEnumGraphSchema>();
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

    public class AutoDetectEnumGraphSchema : Schema
    {
        public AutoDetectEnumGraphSchema(IServiceProvider services)
            : base(services)
        {
            Query = new AutoDetectEnumGraphQuery();
        }
    }

    public class AutoDetectEnumGraphQuery : ObjectGraphType
    {
        public AutoDetectEnumGraphQuery()
        {
            Field<EnumerationGraphType<AutoDetectEnum>>(
                "grumpy",
                resolve: ctx =>
                {
                    return AutoDetectEnum.Grumpy;
                });
        }
    }

    public enum AutoDetectEnum
    {
        Grumpy = 0,
        Happy = 1,
        Sleepy = 2,
    }
}
