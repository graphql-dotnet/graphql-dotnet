using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.DataLoader;
using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class SerialExecutionStrategyTests
    {
        [Fact]
        public async Task VerifyCorrectExecutionOrder()
        {
            var sb = new StringBuilder();
            Func<IResolveFieldContext, object> resolver = context =>
            {
                sb.AppendLine(string.Join(".", context.ResponsePath.Select(x => x.ToString())));
                return "test";
            };
            var leaderGraphType = new ObjectGraphType()
            {
                Name = "LoaderType"
            };
            leaderGraphType.Field<StringGraphType>("lastName", resolve: resolver);
            leaderGraphType.Field<StringGraphType>("name", resolve: resolver);
            var familiesGraphType = new ObjectGraphType()
            {
                Name = "FamiliesType"
            };
            familiesGraphType.Field("leader", leaderGraphType, resolve: resolver);
            familiesGraphType.Field("leader_dataLoader", leaderGraphType, resolve: context =>
            {
                resolver(context);
                return new SimpleDataLoader<object>(ctx =>
                {
                    sb.AppendLine(string.Join(".", context.ResponsePath.Select(x => x.ToString())) + "-completed");
                    return Task.FromResult<object>("test");
                });
            });
            var queryGraphType = new ObjectGraphType();
            queryGraphType.Field("families", new ListGraphType(familiesGraphType), resolve: context =>
            {
                resolver(context);
                return new object[] { "a", "a", "a" };
            });
            var schema = new Schema
            {
                Query = queryGraphType,
                Mutation = queryGraphType,
            };
            var documentExecuter = new DocumentExecuter();
            var executionOptions = new ExecutionOptions()
            {
                Schema = schema,
                Query = "mutation { families { leader_dataLoader { lastName name } leader { lastName name } } }",
            };
            await documentExecuter.ExecuteAsync(executionOptions);
            sb.ToString().ShouldBeCrossPlat(@"families
families.0.leader_dataLoader
families.0.leader
families.0.leader.lastName
families.0.leader.name
families.1.leader_dataLoader
families.1.leader
families.1.leader.lastName
families.1.leader.name
families.2.leader_dataLoader
families.2.leader
families.2.leader.lastName
families.2.leader.name
families.0.leader_dataLoader-completed
families.1.leader_dataLoader-completed
families.2.leader_dataLoader-completed
families.0.leader_dataLoader.lastName
families.0.leader_dataLoader.name
families.1.leader_dataLoader.lastName
families.1.leader_dataLoader.name
families.2.leader_dataLoader.lastName
families.2.leader_dataLoader.name
");
        }

        
    }
}
