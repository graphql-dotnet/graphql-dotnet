namespace GraphQL.Analyzers.SourceGenerators.Models;

/// <summary>
/// Union struct containing data for one generated type entry.
/// Only one of the three properties will be populated; the others will be null.
/// </summary>
public readonly record struct GeneratedTypeEntry(
    SchemaClassData? SchemaClass,
    OutputGraphTypeData? OutputGraphType,
    InputGraphTypeData? InputGraphType,
    string? Namespace,
    ImmutableEquatableArray<PartialClassInfo> PartialClassHierarchy);

/// <summary>
/// Information about a partial class in the hierarchy, including its name and accessibility.
/// </summary>
public readonly record struct PartialClassInfo(
    string ClassName,
    bool IsPublic);

/// <summary>
/// Primitive-only data for an output (object/interface) graph type.
/// Contains all information needed for source generation without requiring ISymbol access.
/// </summary>
public record class OutputGraphTypeData(
    bool IsInterface,
    string FullyQualifiedClrTypeName,
    string GraphTypeClassName,
    ImmutableEquatableArray<OutputMemberData> SelectedMembers,
    InstanceSource InstanceSource,
    ConstructorData? ConstructorData);

/// <summary>
/// Data for a member of an output graph type (field, property, or method).
/// </summary>
public record class OutputMemberData(
    string? DeclaringTypeFullyQualifiedName,
    string MemberName,
    MemberKind MemberKind,
    bool IsStatic,
    ImmutableEquatableArray<MethodParameterData> MethodParameters);

/// <summary>
/// Data for a method parameter.
/// </summary>
public record class MethodParameterData(
    string FullyQualifiedTypeName);

/// <summary>
/// Kind of member (field, property, or method).
/// </summary>
public enum MemberKind
{
    Field = 0,
    Property = 1,
    Method = 2
}

/// <summary>
/// Instance source enumeration matching GraphQL.InstanceSource.
/// </summary>
public enum InstanceSource
{
    ContextSource = 0,
    GetServiceOrCreateInstance = 1,
    GetRequiredService = 2,
    NewInstance = 3
}

/// <summary>
/// Constructor data for types that need instantiation.
/// </summary>
public record class ConstructorData(
    ImmutableEquatableArray<ConstructorParameterData> Parameters,
    ImmutableEquatableArray<RequiredPropertyData> RequiredProperties);

/// <summary>
/// Data for a constructor parameter.
/// </summary>
public record class ConstructorParameterData(
    string? FullyQualifiedTypeName);

/// <summary>
/// Data for a required property.
/// </summary>
public record class RequiredPropertyData(
    string Name,
    string FullyQualifiedTypeName);

/// <summary>
/// Primitive-only data for an input graph type.
/// Contains all information needed for source generation without requiring ISymbol access.
/// </summary>
public record class InputGraphTypeData(
    string FullyQualifiedClrTypeName,
    string GraphTypeClassName,
    ImmutableEquatableArray<InputMemberData> Members,
    ImmutableEquatableArray<InputConstructorParameterData> ConstructorParameters);

/// <summary>
/// Data for a member of an input graph type.
/// </summary>
public record class InputMemberData(
    string? DeclaringTypeFullyQualifiedName,
    string MemberName,
    string FullyQualifiedTypeName);

/// <summary>
/// Data for an input type constructor parameter.
/// </summary>
public record class InputConstructorParameterData(
    string MemberName);

/// <summary>
/// Data for a list element type, including nullability information.
/// </summary>
public record struct ListElementTypeData(
    string ElementTypeName,
    bool IsNullable);

/// <summary>
/// Primitive-only data for the schema class.
/// Contains all information needed for source generation without requiring ISymbol access.
/// </summary>
public record class SchemaClassData(
    bool HasConstructor,
    ImmutableEquatableArray<RegisteredGraphTypeData> RegisteredGraphTypes,
    ImmutableEquatableArray<TypeMappingData> TypeMappings,
    string? QueryRootTypeName,
    string? MutationRootTypeName,
    string? SubscriptionRootTypeName,
    ImmutableEquatableArray<ListElementTypeData> ArrayListTypes,
    ImmutableEquatableArray<ListElementTypeData> GenericListTypes,
    ImmutableEquatableArray<ListElementTypeData> HashSetTypes);

/// <summary>
/// Data for a registered graph type.
/// </summary>
public record class RegisteredGraphTypeData(
    string FullyQualifiedGraphTypeName,
    string? AotGeneratedTypeName,
    string? OverrideTypeName);

/// <summary>
/// Data for a CLR to GraphType mapping.
/// </summary>
public record class TypeMappingData(
    string FullyQualifiedClrTypeName,
    string FullyQualifiedGraphTypeName);
