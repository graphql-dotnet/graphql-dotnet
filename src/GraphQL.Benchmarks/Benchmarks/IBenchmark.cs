namespace GraphQL.Benchmarks;

internal interface IBenchmark
{
    void GlobalSetup();

    void RunProfiler();
}
