using System.Runtime.CompilerServices;
using GraphQL.Tests;

[assembly: GraphQL.OptionalType(typeof(CustomOptional<>))]
[assembly: GraphQL.OptionalType(typeof(StrongBox<>))]
[module: GraphQL.OptionalType(typeof(CustomOptionalClass<>))]

namespace GraphQL.Tests;

public readonly struct CustomOptional<T>
{
    public CustomOptional(T value)
    {
        Data = value;
        Present = true;
    }

    public bool Present { get; }
    public T Data { get; }
}

public sealed class CustomOptionalClass<T>
{
    public CustomOptionalClass() { }

    public CustomOptionalClass(object value) => throw new InvalidOperationException("The constructor accepting T must be selected.");

    public CustomOptionalClass(T value)
    {
        Data = value;
        Present = true;
    }

    public bool Present { get; }
    public T Data { get; } = default!;
}
