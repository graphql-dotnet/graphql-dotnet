using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

public class Bug781UnobservedTasks
{
    private bool _unobserved;

    [Theory(Skip = "for demonstration purposes only, unreliable results")]
    [InlineData("{ do(throwCanceled: false) cancellation never }", true)]
    [InlineData("{ do(throwCanceled: true) cancellation never }", false)]
    public async Task cancel_execution_with_different_error_types(string query, bool unobserved)
    {
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        using var cts = new CancellationTokenSource();
        ISchema schema = new Bug781Schema(cts);

        try
        {
            _ = await schema.ExecuteAsync(options =>
            {
                options.Query = query;
                options.CancellationToken = cts.Token;
                options.ThrowOnUnhandledException = true; // required
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        GC.Collect(); // GC causes UnobservedTaskException event
        await Task.Delay(1000).ConfigureAwait(false); // Wait some time for GC to complete

        TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;

        _unobserved.ShouldBe(unobserved);
    }

    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        _unobserved = true;
        Console.WriteLine("Unobserved exception: " + e.Exception);
    }
}

public class Bug781Schema : Schema
{
    public Bug781Schema(CancellationTokenSource cts)
    {
        Query = new Bug781Query(cts);
    }
}

public class Bug781Query : ObjectGraphType
{
    public Bug781Query(CancellationTokenSource cts)
    {
        // First field with long calculation emulation
        FieldAsync<StringGraphType>(
            "do",
            arguments: new QueryArguments(new QueryArgument<BooleanGraphType> { Name = "throwCanceled" }),
            resolve: async ctx =>
            {
                await Task.Delay(10).ConfigureAwait(false);
                cts.Token.WaitHandle.WaitOne();

                if (ctx.GetArgument<bool>("throwCanceled"))
                    throw new OperationCanceledException(); // in this case the task will go into Canceled state and there will be no unobserved exception event
                else
                    throw new Exception("Hello!"); // the task goes into Faulted state so unobserved exception event will arise
            });

        // Second field causes cancellation of execution
        Field<StringGraphType>(
           "cancellation",
           resolve: ctx =>
           {
               cts.Cancel();
               return "cancelled";
           });

        // The third field is necessary for the control to fall on the context.CancellationToken.ThrowIfCancellationRequested() instruction.
        Field<StringGraphType>(
           "never",
           resolve: ctx => throw new Exception("Never called"));
    }
}
