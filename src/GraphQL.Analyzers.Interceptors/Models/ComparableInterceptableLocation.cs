using Microsoft.CodeAnalysis.CSharp;

namespace GraphQL.Analyzers.Interceptors.Models;

public record ComparableInterceptableLocation
{
    public ComparableInterceptableLocation(InterceptableLocation location)
    {
        Version = location.Version;
        Data = location.Data;
    }

    public int Version { get; }
    public string Data { get; }

    public string GetDisplayLocation()
    {
        return Data.Replace("+", "_1").Replace("/", "_2").TrimEnd('=');
    }

    public string GetInterceptsLocationAttributeSyntax()
        => $"""[global::System.Runtime.CompilerServices.InterceptsLocationAttribute({Version}, "{Data}")]""";
}
