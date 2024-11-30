namespace Example;

public class GraphQlSettings
{
    public PathString GraphQlPath { get; set; }
    public Func<HttpContext, IDictionary<string, object>> BuildUserContext { get; set; } = null!;
    public bool EnableMetrics { get; set; }
    public bool ExposeExceptions { get; set; }
}
