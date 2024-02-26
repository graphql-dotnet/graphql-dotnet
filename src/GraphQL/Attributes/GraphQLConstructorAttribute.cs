namespace GraphQL;

/// <summary>
/// When placed on a constructor, indicates that the constructor should be used to create instances of the type on deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
public class GraphQLConstructorAttribute : Attribute
{
}
