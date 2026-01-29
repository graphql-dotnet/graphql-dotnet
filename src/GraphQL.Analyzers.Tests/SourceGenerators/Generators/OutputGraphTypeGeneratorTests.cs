using GraphQL.Analyzers.SourceGenerators.Generators;
using GraphQL.Analyzers.SourceGenerators.Models;

namespace GraphQL.Analyzers.Tests.SourceGenerators.Generators;

/// <summary>
/// Unit tests for OutputGraphTypeGenerator that generates AOT output graph type code.
/// </summary>
public class OutputGraphTypeGeneratorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GeneratesComprehensiveOutputGraphTypes(bool isInterface)
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", Accessibility: ClassAccessibility.Public)
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
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
                MemberName: "Name",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Method with parameters
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
                MemberName: "GetFriends",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                IsSourceStreamResolver: false,
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
                IsSourceStreamResolver: false,
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
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Static property
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
                MemberName: "Version",
                MemberKind: MemberKind.Property,
                IsStatic: true,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Static method with no parameters
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.IStarWarsCharacter",
                MemberName: "GetAllCharacterTypes",
                MemberKind: MemberKind.Method,
                IsStatic: true,
                IsSourceStreamResolver: false,
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
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "Name",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Method with single parameter
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "GetFriends",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                IsSourceStreamResolver: false,
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
                IsSourceStreamResolver: false,
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
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Property
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "PrimaryFunction",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Field
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "_manufacturer",
                MemberKind: MemberKind.Field,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Static property
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "DefaultModel",
                MemberKind: MemberKind.Property,
                IsStatic: true,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Static method with parameters
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "CreateDroid",
                MemberKind: MemberKind.Method,
                IsStatic: true,
                IsSourceStreamResolver: false,
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
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
        });

        var objectType = new OutputGraphTypeData(
            IsInterface: false,
            FullyQualifiedClrTypeName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
            GraphTypeClassName: "AutoOutputGraphType_Droid",
            SelectedMembers: objectMembers,
            InstanceSource: GraphQL.Analyzers.SourceGenerators.Models.InstanceSource.ContextSource,
            ConstructorData: null);

        // Act - Generate type
        var result = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, isInterface ? interfaceType : objectType);

        // Assert - Interface type
        result.ShouldMatchApproved(o => o.NoDiff().WithDiscriminator(isInterface ? "Interface" : "Object"));
    }

    [Fact]
    public void GeneratesEmptyOutputGraphType()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", Accessibility: ClassAccessibility.Public)
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
            new PartialClassInfo("OuterClass", Accessibility: ClassAccessibility.Public),
            new PartialClassInfo("InnerClass", Accessibility: ClassAccessibility.Internal),
            new PartialClassInfo("DeepestClass", Accessibility: ClassAccessibility.Public)
        });

        var members = new ImmutableEquatableArray<OutputMemberData>(new[]
        {
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.Person",
                MemberName: "Name",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                IsSourceStreamResolver: false,
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
            new PartialClassInfo("SampleAotSchema", Accessibility: ClassAccessibility.Public)
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
            new PartialClassInfo("SampleAotSchema", Accessibility: ClassAccessibility.Public)
        });

        // Droid object type matching the user's example
        var droidMembers = new ImmutableEquatableArray<OutputMemberData>(new[]
        {
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "Id",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "Name",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "GetFriends",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                {
                    new MethodParameterData("global::GraphQL.StarWars.TypeFirst.StarWarsData")
                })),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "GetFriendsConnection",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                {
                    new MethodParameterData("global::GraphQL.StarWars.TypeFirst.StarWarsData")
                })),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "AppearsIn",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.Droid",
                MemberName: "PrimaryFunction",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                IsSourceStreamResolver: false,
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

    [Fact]
    public void GeneratesGetRequiredServiceInstance()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", Accessibility: ClassAccessibility.Public)
        });

        var members = new ImmutableEquatableArray<OutputMemberData>(new[]
        {
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.Person",
                MemberName: "Name",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
        });

        var outputType = new OutputGraphTypeData(
            IsInterface: false,
            FullyQualifiedClrTypeName: "global::Sample.Person",
            GraphTypeClassName: "AutoOutputGraphType_Person",
            SelectedMembers: members,
            InstanceSource: GraphQL.Analyzers.SourceGenerators.Models.InstanceSource.GetRequiredService,
            ConstructorData: null);

        // Act
        var result = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, outputType);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratesNewInstanceWithConstructor(bool hasRequiredProperties)
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", Accessibility: ClassAccessibility.Public)
        });

        var members = new ImmutableEquatableArray<OutputMemberData>(new[]
        {
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.Person",
                MemberName: "Name",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
        });

        var constructorParams = new ImmutableEquatableArray<ConstructorParameterData>(new[]
        {
            new ConstructorParameterData("global::System.String"),
            new ConstructorParameterData("global::System.Int32")
        });

        var requiredProperties = hasRequiredProperties
            ? new ImmutableEquatableArray<RequiredPropertyData>(new[]
            {
                new RequiredPropertyData("Email", "global::System.String"),
                new RequiredPropertyData("Age", "global::System.Int32")
            })
            : ImmutableEquatableArray<RequiredPropertyData>.Empty;

        var constructorData = new ConstructorData(constructorParams, requiredProperties);

        var outputType = new OutputGraphTypeData(
            IsInterface: false,
            FullyQualifiedClrTypeName: "global::Sample.Person",
            GraphTypeClassName: "AutoOutputGraphType_Person",
            SelectedMembers: members,
            InstanceSource: GraphQL.Analyzers.SourceGenerators.Models.InstanceSource.NewInstance,
            ConstructorData: constructorData);

        // Act
        var result = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, outputType);

        // Assert
        var discriminator = hasRequiredProperties ? "WithRequiredProperties" : "WithoutRequiredProperties";
        result.ShouldMatchApproved(o => o.NoDiff().WithDiscriminator(discriminator));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratesGetServiceOrCreateInstance(bool hasRequiredProperties)
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", Accessibility: ClassAccessibility.Public)
        });

        var members = new ImmutableEquatableArray<OutputMemberData>(new[]
        {
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.Person",
                MemberName: "Name",
                MemberKind: MemberKind.Property,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
        });

        var constructorParams = new ImmutableEquatableArray<ConstructorParameterData>(new[]
        {
            new ConstructorParameterData("global::System.String"),
            new ConstructorParameterData("global::System.Int32")
        });

        var requiredProperties = hasRequiredProperties
            ? new ImmutableEquatableArray<RequiredPropertyData>(new[]
            {
                new RequiredPropertyData("Email", "global::System.String"),
                new RequiredPropertyData("Age", "global::System.Int32")
            })
            : ImmutableEquatableArray<RequiredPropertyData>.Empty;

        var constructorData = new ConstructorData(constructorParams, requiredProperties);

        var outputType = new OutputGraphTypeData(
            IsInterface: false,
            FullyQualifiedClrTypeName: "global::Sample.Person",
            GraphTypeClassName: "AutoOutputGraphType_Person",
            SelectedMembers: members,
            InstanceSource: GraphQL.Analyzers.SourceGenerators.Models.InstanceSource.GetServiceOrCreateInstance,
            ConstructorData: constructorData);

        // Act
        var result = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, outputType);

        // Assert
        var discriminator = hasRequiredProperties ? "WithRequiredProperties" : "WithoutRequiredProperties";
        result.ShouldMatchApproved(o => o.NoDiff().WithDiscriminator(discriminator));
    }

    [Fact]
    public void GeneratesSubscriptionGraphType()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", Accessibility: ClassAccessibility.Public)
        });

        // Subscription type with source stream resolvers
        var subscriptionMembers = new ImmutableEquatableArray<OutputMemberData>(new[]
        {
            // IObservable<T> - source stream resolver
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.Subscription",
                MemberName: "OnMessage",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                IsSourceStreamResolver: true,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
            // Task<IObservable<T>> - source stream resolver
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.Subscription",
                MemberName: "OnNotification",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                IsSourceStreamResolver: true,
                MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                {
                    new MethodParameterData("string")
                })),
            // IAsyncEnumerable<T> - source stream resolver
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.Subscription",
                MemberName: "OnEvent",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                IsSourceStreamResolver: true,
                MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                {
                    new MethodParameterData("int")
                })),
            // Regular method - not a source stream resolver
            new OutputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.Subscription",
                MemberName: "GetStatus",
                MemberKind: MemberKind.Method,
                IsStatic: false,
                IsSourceStreamResolver: false,
                MethodParameters: ImmutableEquatableArray<MethodParameterData>.Empty),
        });

        var subscriptionType = new OutputGraphTypeData(
            IsInterface: false,
            FullyQualifiedClrTypeName: "global::Sample.Subscription",
            GraphTypeClassName: "AutoOutputGraphType_Subscription",
            SelectedMembers: subscriptionMembers,
            InstanceSource: GraphQL.Analyzers.SourceGenerators.Models.InstanceSource.ContextSource,
            ConstructorData: null);

        // Act
        var result = OutputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, subscriptionType);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }
}
