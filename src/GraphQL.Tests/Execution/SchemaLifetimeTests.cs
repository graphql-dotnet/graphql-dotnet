using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using Should.Core.Assertions;

namespace GraphQL.Tests.Execution
{
    public class SchemaLifetimeTests
    {
        [Test]
        public void DisposingRightAway_DoesNotThrowException()
        {
            var schema = new Schema();

            Assert.DoesNotThrow(() => schema.Dispose());
        }

        [Test]
        public async Task ExecutingThenDisposing_DoesNotThrowException()
        {
            var executer = new DocumentExecuter(new AntlrDocumentBuilder(), new DocumentValidator());
            var schema = new Schema();

            await executer.ExecuteAsync(schema, null, "{noop}", null);

            Assert.DoesNotThrow(() => schema.Dispose());
        }
    }
}