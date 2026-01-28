using GraphQL.Analyzers.SourceGenerators.Generators;
using GraphQL.Analyzers.SourceGenerators.Models;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/// <summary>
/// Unit tests for OutputGraphTypeGenerator that generates AOT output graph type code.
/// </summary>
public class OutputGraphTypeGeneratorTests
{
    [Fact]
    public void GeneratesComprehensiveOutputGraphTypes()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
        });

        // Test both interface and object types with comprehensive member sets
        // Interface type with properties and methods
        var interfaceMembers = new ImmutableEquatableArray<OutputMemberData>(new[]
        {
            // Simple properties
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
                MemberName: "Id",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
                MemberName: "Name",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Method with parameters
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
                MemberName: "GetFriends",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                {
                    new MethodParameterData("global::GraphQL.StarWars.TypeFirst.StarWarsData")
                })),
            // Method with multiple parameters
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
                MemberName: "GetFriendsConnection",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                {
                    new MethodParameterData("global::GraphQL.StarWars.TypeFirst.StarWarsData"),
                    new MethodParameterData("int?")
                })),
            // Property array
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
                MemberName: "AppearsIn",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Static property
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
                MemberName: "Version",
                MemberKind: MemberKind.Property,
                IsStatic: true,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Static method with no parameters
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
                MemberName: "GetAllCharacterTypes",
                MemberKind: MemberKind.Method,
                IsStatic: true,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
        });

        var interfaceType = new OutputGraphTypeData(
            IsInterface: true,
            FullyQualifiedClrTypeName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
            GraphTypeClassName: "AutoOutputGraphType_IStarWarsCharacter",
            SelectedMembers: interfaceMembers,
            InstanceSource: GraphQL.Analyzers.SourceGenerators.Models.InstanceSource.ContextSource,
            ConstructorData: null);

        // Object type with properties, fields, and methods
        var objectMembers = new ImmutableEquatableArray<OutputMemberData>(new[]
        {
            // Properties
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "Id",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "Name",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Method with single parameter
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "GetFriends",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                {
                    new MethodParameterData("global::GraphQL.StarWars.TypeFirst.StarWarsData")
                })),
            // Method with multiple parameters
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "GetFriendsConnection",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                {
                    new MethodParameterData("global::GraphQL.StarWars.TypeFirst.StarWarsData"),
                    new MethodParameterData("int?"),
                    new MethodParameterData("string?")
                })),
            // Property
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "AppearsIn",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Property
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "PrimaryFunction",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Field
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "_manufacturer",
                MemberKind: MemberKind.Field,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Static property
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "DefaultModel",
                MemberKind: MemberKind.Property,
                IsStatic: true,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Static method with parameters
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "CreateDroid",
                MemberKind: MemberKind.Method,
                IsStatic: true,
                MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                {
                    new MethodParameterData("string"),
                    new MethodParameterData("string")
                })),
            // Instance method with no parameters
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "GetSerialNumber",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
        });

        var objectType = new OutputGraphTypeData(
            IsInterface: false,
            FullyQualifiedClrTypeName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
            GraphTypeClassName: "AutoOutputGraphType_Droid",
            SelectedMembers: objectMembers,
            InstanceSource: GraphQL.Analyzers.SourceGenerators.Models.InstanceSource.ContextSource,
            ConstructorData: null);

        // Act - Generate interface type
        var interfaceResult = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, interfaceType);

        // Assert - Interface type
        interfaceResult.ShouldMatchApproved(o => o.NoDiff().WithDiscriminator("Interface"));

        // Act - Generate object type
        var objectResult = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, objectType);

        // Assert - Object type
        objectResult.ShouldMatchApproved(o => o.NoDiff().WithDiscriminator("Object"));
    }

    [Fact]
    public void GeneratesEmptyOutputGraphType()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
        });

        var outputType = new OutputGraphTypeData(
            IsInterface: false,
            FullyQualifiedClrTypeName: "global::Sample.EmptyType",
            GraphTypeClassName: "AutoOutputGraphType_EmptyType",
            SelectedMembers: ImmutableEquatableArray<OutputMemberData>.Empty,
            InstanceSource: GraphQL.Analyzers.SourceGenerators.Models.InstanceSource.ContextSource,
            ConstructorData: null);

        // Act
        var result = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, outputType);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void GeneratesNestedPartialClasses()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("OuterClass", IsPublic: true),
            new PartialClassInfo("InnerClass", IsPublic: false),
            new PartialClassInfo("DeepestClass", IsPublic: true)
        });

        var members = new ImmutableEquatableArray<OutputMemberData>(new[]
        {
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.Person",
                MemberName: "Name",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
        });

        var outputType = new OutputGraphTypeData(
            IsInterface: false,
            FullyQualifiedClrTypeName: "global::Sample.Person",
            GraphTypeClassName: "AutoOutputGraphType_Person",
            SelectedMembers: members,
            InstanceSource: GraphQL.Analyzers.SourceGenerators.Models.InstanceSource.ContextSource,
            ConstructorData: null);

        // Act
        var result = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, outputType);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void ReturnsEmptyStringForNullData()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
        });

        // Act
        var result = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, null);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void ReturnsEmptyStringForEmptyHierarchy()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = ImmutableEquatableArray<PartialClassInfo>.Empty;
        var outputType = new OutputGraphTypeData(
            IsInterface: false,
            FullyQualifiedClrTypeName: "global::Sample.Person",
            GraphTypeClassName: "AutoOutputGraphType_Person",
            SelectedMembers: ImmutableEquatableArray<OutputMemberData>.Empty,
            InstanceSource: GraphQL.Analyzers.SourceGenerators.Models.InstanceSource.ContextSource,
            ConstructorData: null);

        // Act
        var result = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, outputType);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void GeneratesDroidObjectType()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
        });

        // Droid object type matching the user's example
        var droidMembers = new ImmutableEquatableArray<OutputMemberData>(new[]
        {
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "Id",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "Name",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "GetFriends",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                {
                    new MethodParameterData("global::GraphQL.StarWars.TypeFirst.StarWarsData")
                })),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "GetFriendsConnection",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                {
                    new MethodParameterData("global::GraphQL.StarWars.TypeFirst.StarWarsData")
                })),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "AppearsIn",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "PrimaryFunction",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
        });

        var droidType = new OutputGraphTypeData(
            IsInterface: false,
            FullyQualifiedClrTypeName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
            GraphTypeClassName: "AutoOutputGraphType_Droid",
            SelectedMembers: droidMembers,
            InstanceSource: GraphQL.Analyzers.SourceGenerators.Models.InstanceSource.ContextSource,
            ConstructorData: null);

        // Act
        var result = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, droidType);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }
}
