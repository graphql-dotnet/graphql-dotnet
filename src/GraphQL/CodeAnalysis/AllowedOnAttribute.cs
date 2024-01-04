using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Marker attribute for analyzer usage to indicate
/// graph types where annotated method is allowed to be used
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal class AllowedOnAttribute<T> : Attribute where T : IComplexGraphType { }

/// <inheritdoc cref="AllowedOnAttribute{T}"/>
[AttributeUsage(AttributeTargets.Method)]
internal class AllowedOnAttribute<T1, T2> : Attribute where T1 : IComplexGraphType where T2 : IComplexGraphType { }
