#nullable enable

using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests;

internal static class ObjectExtensions
{
    public static T? ToObject<T>(this IDictionary<string, object?> data, bool compile = false)
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<QueryT<T>>());
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        var type = schema.Query.Fields.Find("test")!.Arguments!.Single().ResolvedType!;
        if (compile)
            return (T?)GraphQL.ObjectExtensions.CompileToObject(typeof(T), (IInputObjectGraphType)type.GetNamedType())(data);
        return (T?)data.ToObject(typeof(T), type);
    }

    private class QueryT<T>
    {
        public virtual string? Test(T value) => null;
    }
}
