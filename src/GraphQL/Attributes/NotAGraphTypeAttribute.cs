using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Marker attribute for analyzer usage to indicate the generic
/// parameter is not <see cref="IGraphType"/>
/// </summary>
[AttributeUsage(AttributeTargets.GenericParameter)]
internal class NotAGraphTypeAttribute : Attribute { }
