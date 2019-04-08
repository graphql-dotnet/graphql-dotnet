using System.Threading.Tasks;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class SchemaLifetimeTests
    {
        [Fact]
        public void DisposingRightAway_DoesNotThrowException()
        {
            var schema = new Schema();

            Should.NotThrow(() => schema.Dispose());
        }

        [Fact]
        public async Task ExecutingThenDisposing_DoesNotThrowException()
        {
            var executor = new DocumentExecutor();
            var schema = new Schema();

            await executor.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = "{noop}";
            }).ConfigureAwait(false);

            Should.NotThrow(() => schema.Dispose());
        }
    }
}
