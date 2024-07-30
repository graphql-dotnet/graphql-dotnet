namespace GraphQL;

/// <summary>
/// Marker attribute for analyzer usage to indicate the generic
/// argument or interface implementation type must have
/// parameterless constructor
/// </summary>
[AttributeUsage(AttributeTargets.GenericParameter | AttributeTargets.Interface)]
internal class RequireParameterlessConstructorAttribute : Attribute;
