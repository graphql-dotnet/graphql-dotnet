using System.Threading.Tasks;
using GraphQL.Types;
using Should.Core.Assertions;

namespace GraphQL.Tests.Execution
{
    public class SchemaLifetimeTests
    {
        [Fact]
        public void DisposingRightAway_DoesNotThrowException()
        {
            var schema = new Schema();

            Assert.DoesNotThrow(() => schema.Dispose());
        }

        [Fact]
        public async Task ExecutingThenDisposing_DoesNotThrowException()
        {
            var executer = new DocumentExecuter();
            var schema = new Schema();

            await executer.ExecuteAsync(schema, null, "{noop}", null).ConfigureAwait(false);

            Assert.DoesNotThrow(() => schema.Dispose());
        }
    }
}
