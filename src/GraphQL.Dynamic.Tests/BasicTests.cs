using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Dynamic.Tests.Schemas;
using GraphQL.Dynamic.Types.Introspection;
using GraphQL.Dynamic.Types.LiteralGraphType;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GraphQL.Dynamic.Tests
{
    public class BasicTests
    {
        private readonly Schema _schema;

        public BasicTests()
        {
            _schema = Root.FromJson(GetFileContentsForTestFile("./Resources/SampleRemoteSchema.json")).Data.Schema;
        }

        [Fact]
        public async Task Can_Execute_Simple_Remote_Query()
        {
            // A moniker uniquely identifies a remote resource
            var moniker = DynamicSampleRemoteSchema.GithubMoniker;

            var remotes = new[]
            {
                new RemoteDescriptor
                {
                    Moniker = moniker,
                    Url = "foobar"
                }
            };

            // Generate the new types for the remote
            // We're passing an override for remoteSchemaFetcher so we don't actually call the remote introspection endpoint
            var types = await RemoteLiteralGraphType.LoadRemotes(remotes, remoteSchemaFetcher: url => _schema);

            var executor = new DocumentExecuter();
            var query = @"
            {
                githubDynamic {
                    user {
                        id
                        login
                        company
                        repos {
                            id
                            name                                
                        }
                    }
                }
            }";

            var result = await executor.ExecuteAsync(new ExecutionOptions
            {
                Schema = new DynamicSampleRemoteSchema(types),
                Query = query
            });

            var queryResult = JObject.FromObject(result.Data);

            Assert.NotNull(queryResult.SelectToken("githubDynamic.user").Value<object>());
            Assert.NotNull(queryResult.SelectToken("githubDynamic.user.login").Value<object>());
            Assert.NotNull(queryResult.SelectToken("githubDynamic.user.id").Value<object>());
            Assert.NotNull(queryResult.SelectToken("githubDynamic.user.company").Value<object>());
            Assert.NotNull(queryResult.SelectToken("githubDynamic.user.repos").Value<object>());
            Assert.NotNull(queryResult.SelectToken("githubDynamic.user.repos[0].id").Value<object>());
            Assert.NotNull(queryResult.SelectToken("githubDynamic.user.repos[0].name").Value<object>());
            Assert.NotNull(queryResult.SelectToken("githubDynamic.user.repos[1].id").Value<object>());
            Assert.NotNull(queryResult.SelectToken("githubDynamic.user.repos[1].name").Value<object>());
        }

        private string GetFileContentsForTestFile(string relativePath) => File.ReadAllText(Path.Combine(Path.GetDirectoryName(Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath)), relativePath));
    }
}
