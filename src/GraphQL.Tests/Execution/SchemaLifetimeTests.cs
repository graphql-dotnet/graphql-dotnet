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
            var executer = new DocumentExecuter();
            var schema = new Schema();

            await executer.ExecuteAsync(schema, null, "{noop}", null).ConfigureAwait(false);

            Should.NotThrow(() => schema.Dispose());
        }
    }
}
