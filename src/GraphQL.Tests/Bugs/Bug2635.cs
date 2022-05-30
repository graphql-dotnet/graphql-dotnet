using GraphQL.Types;

namespace GraphQL.Tests.Bugs;

// https://github.com/graphql-dotnet/graphql-dotnet/issues/2635
public class Bug2635 : QueryTestBase<Bug2635.MySchema>
{
    public int Num;
    public CancellationTokenSource CancellationTokenSource;

    [Fact]
    public async Task test_parallel()
    {
        Num = 0;
        CancellationTokenSource = new CancellationTokenSource();
        var de = new DocumentExecuter();
        try
        {
            _ = await de.ExecuteAsync(new ExecutionOptions
            {
                Query = "{a b}",
                Schema = new MySchema(),
                Root = this,
                CancellationToken = CancellationTokenSource.Token,
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        Num.ShouldBe(1);
    }

    public class MySchema : Schema
    {
        public MySchema()
        {
            Query = new MyQuery();
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            FieldAsync<IntGraphType>("a", resolve: async context =>
            {
                await Task.Delay(500).ConfigureAwait(false);
                ((Bug2635)context.RootValue).Num = 1;
                throw new Exception();
            });
            Field<IntGraphType>("b", resolve: context =>
            {
                ((Bug2635)context.RootValue).CancellationTokenSource.Cancel();
                context.CancellationToken.ThrowIfCancellationRequested();
                return 2;
            });
        }
    }
}
