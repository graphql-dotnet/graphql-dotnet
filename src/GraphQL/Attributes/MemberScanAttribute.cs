namespace GraphQL;

/// <summary>
/// Specifies which member types should be scanned when building a GraphQL graph type.
/// This attribute can be applied to classes to control whether properties, fields, and/or methods
/// are scanned during auto-registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class MemberScanAttribute : GraphQLAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="MemberScanAttribute"/> with the specified member types to scan.
    /// </summary>
    /// <param name="memberTypes">The types of members to scan. Can be combined using bitwise OR.</param>
    public MemberScanAttribute(ScanMemberTypes memberTypes)
    {
        MemberTypes = memberTypes;
    }

    /// <summary>
    /// Gets the member types that should be scanned.
    /// </summary>
    public ScanMemberTypes MemberTypes { get; }
}

/// <summary>
/// Specifies the types of members that can be scanned during auto-registration.
/// </summary>
[Flags]
public enum ScanMemberTypes
{
    /// <summary>
    /// Properties should be scanned.
    /// </summary>
    Properties = 1,

    /// <summary>
    /// Fields should be scanned.
    /// </summary>
    Fields = 2,

    /// <summary>
    /// Methods should be scanned.
    /// </summary>
    Methods = 4,
}
