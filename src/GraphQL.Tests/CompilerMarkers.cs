#if !NET11_0_OR_GREATER

// C# unions and closed type hierarchies are language features rather than runtime features, so the compiler
// accepts them on any target framework as long as the marker types it lowers them to are available. They only
// ship in the framework from net11.0 onwards, so below that they are supplied here. That is what a consumer on
// an earlier framework does too, typically through a polyfill package.
//
// It also makes these tests meaningful downlevel. The library targets netstandard2.0 and net6.0, so it can
// never hold a compile time reference to these types, and the copy declared here is a different Type from the
// one in the framework. A green run below net11.0 is therefore what proves the markers are matched by name.

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
internal sealed class UnionAttribute : Attribute
{
}

internal interface IUnion
{
    object? Value { get; }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class IsClosedTypeAttribute : Attribute
{
    private Type[] _derivedTypes = Type.EmptyTypes;

    public Type[] DerivedTypes
    {
        get => _derivedTypes;
        set => _derivedTypes = value ?? Type.EmptyTypes;
    }
}

#endif
