#nullable enable

using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests;

internal static class ObjectExtensions
{
    public static T? ToObject<T>(this IDictionary<string, object?> data, bool compile = false, ValueConverter? valueConverter = null)
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<QueryT<T>>());
        using var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        if (valueConverter != null)
            ((Schema)schema).ValueConverter = valueConverter;
        schema.Initialize();
        var type = schema.Query.Fields.Find("test")!.Arguments!.Single().ResolvedType!;
        var inputType = (IInputObjectGraphType)type.GetNamedType();
        if (compile)
            return (T?)GraphQL.ObjectExtensions.CompileToObject(typeof(T), inputType, schema.ValueConverter)(data);
        return (T?)schema.ValueConverter.ToObject(data, typeof(T), inputType);
    }

    private class QueryT<T>
    {
        public virtual string? Test(T value) => null;
    }
}
