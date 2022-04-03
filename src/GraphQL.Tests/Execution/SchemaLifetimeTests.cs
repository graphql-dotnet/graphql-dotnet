using GraphQL.Types;

namespace GraphQL.Tests.Execution;

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

        await executer.ExecuteAsync(_ =>
        {
            _.Schema = schema;
            _.Query = "{noop}";
        }).ConfigureAwait(false);

        Should.NotThrow(() => schema.Dispose());
    }
}
