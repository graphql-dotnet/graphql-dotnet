#if NETSTANDARD2_0

// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/NullableAttributes.cs#L107
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Applied to a method that will never return under any circumstance.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class DoesNotReturnAttribute : Attribute
{
}

#endif
