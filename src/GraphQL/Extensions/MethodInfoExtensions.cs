#if !NET5_0_OR_GREATER

using System.Reflection;

namespace GraphQL;

internal static class MethodInfoExtensions
{
    public static T CreateDelegate<T>(this MethodInfo methodInfo, object? target)
        where T : Delegate
        => (T)methodInfo.CreateDelegate(typeof(T), target);
}

#endif
