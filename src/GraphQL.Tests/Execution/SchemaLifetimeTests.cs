using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
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
            var executer = new DocumentExecuter(new AntlrDocumentBuilder(), new DocumentValidator());
            var schema = new Schema();

            await executer.ExecuteAsync(schema, null, "{noop}", null).ConfigureAwait(false);

            Assert.DoesNotThrow(() => schema.Dispose());
        }
    }
}
