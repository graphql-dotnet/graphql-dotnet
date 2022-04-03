namespace GraphQL.Benchmarks;

public class IntrospectionResult
{
    public static readonly string Data = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IntrospectionResult.json"));
}
