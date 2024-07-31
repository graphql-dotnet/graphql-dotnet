namespace GraphQL;

/// <summary>
/// Marker attribute for analyzer usage to indicate the interface
/// implementation type must have parameterless constructor
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
internal class RequireParameterlessConstructorAttribute : Attribute;
