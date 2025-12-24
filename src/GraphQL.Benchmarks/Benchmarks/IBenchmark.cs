namespace GraphQL.Benchmarks;

internal interface IBenchmark
{
    public void GlobalSetup();

    public void RunProfiler();
}
