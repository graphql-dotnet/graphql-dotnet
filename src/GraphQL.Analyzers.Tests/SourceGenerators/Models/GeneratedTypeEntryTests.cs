using GraphQL.Analyzers.SourceGenerators.Models;

namespace GraphQL.Analyzers.Tests.SourceGenerators.Models;

/// <summary>
/// Unit tests for GeneratedTypeEntry equality comparisons.
/// </summary>
public class GeneratedTypeEntryTests
{
    [Fact]
    public void GeneratedTypeEntry_SchemaClass_EqualityComparison()
    {
        // Arrange - Create first fully populated GeneratedTypeEntry
        var entry1 = new GeneratedTypeEntry(
            SchemaClass: new SchemaClassData(
                HasConstructor: true,
                RegisteredGraphTypes: new ImmutableEquatableArray<RegisteredGraphTypeData>(new[]
                {
                    new RegisteredGraphTypeData(
                        FullyQualifiedGraphTypeName: "global::Sample.PersonGraphType",
                        AotGeneratedTypeName: "AutoOutputGraphType_Person",
                        OverrideTypeName: null,
                        ConstructorData: new ConstructorData(
                            Parameters: new ImmutableEquatableArray<ConstructorParameterData>(new[]
                            {
                                new ConstructorParameterData("global::System.String"),
                                new ConstructorParameterData("global::System.Int32")
                            }),
                            RequiredProperties: new ImmutableEquatableArray<RequiredPropertyData>(new[]
                            {
                                new RequiredPropertyData("Name", "global::System.String"),
                                new RequiredPropertyData("Age", "global::System.Int32")
                            }))),
                    new RegisteredGraphTypeData(
                        FullyQualifiedGraphTypeName: "global::Sample.ProductGraphType",
                        AotGeneratedTypeName: "AutoOutputGraphType_Product",
                        OverrideTypeName: "CustomProduct",
                        ConstructorData: null)
                }),
                TypeMappings: new ImmutableEquatableArray<TypeMappingData>(new[]
                {
                    new TypeMappingData("global::Sample.Person", "global::Sample.PersonGraphType"),
                    new TypeMappingData("global::Sample.Product", "global::Sample.ProductGraphType")
                }),
                QueryRootTypeName: "Query",
                MutationRootTypeName: "Mutation",
                SubscriptionRootTypeName: "Subscription",
                ArrayListTypes: new ImmutableEquatableArray<ListElementTypeData>(new[]
                {
                    new ListElementTypeData("global::System.String", true),
                    new ListElementTypeData("global::System.Decimal", false)
                }),
                GenericListTypes: new ImmutableEquatableArray<ListElementTypeData>(new[]
                {
                    new ListElementTypeData("global::System.Int32", false),
                    new ListElementTypeData("global::System.Double", true)
                }),
                HashSetTypes: new ImmutableEquatableArray<ListElementTypeData>(new[]
                {
                    new ListElementTypeData("global::System.Boolean", true),
                    new ListElementTypeData("global::System.Guid", false)
                })),
            OutputGraphType: null,
            InputGraphType: null,
            Namespace: "Sample.GraphQL",
            PartialClassHierarchy: new ImmutableEquatableArray<PartialClassInfo>(new[]
            {
                new PartialClassInfo("SampleSchema", ClassAccessibility.Public),
                new PartialClassInfo("NestedClass", ClassAccessibility.Internal)
            }));

        // Arrange - Create second identical GeneratedTypeEntry without reusing nested classes
        var entry2 = new GeneratedTypeEntry(
            SchemaClass: new SchemaClassData(
                HasConstructor: true,
                RegisteredGraphTypes: new ImmutableEquatableArray<RegisteredGraphTypeData>(new[]
                {
                    new RegisteredGraphTypeData(
                        FullyQualifiedGraphTypeName: "global::Sample.PersonGraphType",
                        AotGeneratedTypeName: "AutoOutputGraphType_Person",
                        OverrideTypeName: null,
                        ConstructorData: new ConstructorData(
                            Parameters: new ImmutableEquatableArray<ConstructorParameterData>(new[]
                            {
                                new ConstructorParameterData("global::System.String"),
                                new ConstructorParameterData("global::System.Int32")
                            }),
                            RequiredProperties: new ImmutableEquatableArray<RequiredPropertyData>(new[]
                            {
                                new RequiredPropertyData("Name", "global::System.String"),
                                new RequiredPropertyData("Age", "global::System.Int32")
                            }))),
                    new RegisteredGraphTypeData(
                        FullyQualifiedGraphTypeName: "global::Sample.ProductGraphType",
                        AotGeneratedTypeName: "AutoOutputGraphType_Product",
                        OverrideTypeName: "CustomProduct",
                        ConstructorData: null)
                }),
                TypeMappings: new ImmutableEquatableArray<TypeMappingData>(new[]
                {
                    new TypeMappingData("global::Sample.Person", "global::Sample.PersonGraphType"),
                    new TypeMappingData("global::Sample.Product", "global::Sample.ProductGraphType")
                }),
                QueryRootTypeName: "Query",
                MutationRootTypeName: "Mutation",
                SubscriptionRootTypeName: "Subscription",
                ArrayListTypes: new ImmutableEquatableArray<ListElementTypeData>(new[]
                {
                    new ListElementTypeData("global::System.String", true),
                    new ListElementTypeData("global::System.Decimal", false)
                }),
                GenericListTypes: new ImmutableEquatableArray<ListElementTypeData>(new[]
                {
                    new ListElementTypeData("global::System.Int32", false),
                    new ListElementTypeData("global::System.Double", true)
                }),
                HashSetTypes: new ImmutableEquatableArray<ListElementTypeData>(new[]
                {
                    new ListElementTypeData("global::System.Boolean", true),
                    new ListElementTypeData("global::System.Guid", false)
                })),
            OutputGraphType: null,
            InputGraphType: null,
            Namespace: "Sample.GraphQL",
            PartialClassHierarchy: new ImmutableEquatableArray<PartialClassInfo>(new[]
            {
                new PartialClassInfo("SampleSchema", ClassAccessibility.Public),
                new PartialClassInfo("NestedClass", ClassAccessibility.Internal)
            }));

        // Act
        var areEqual = EqualityComparer<GeneratedTypeEntry>.Default.Equals(entry1, entry2);

        // Assert
        areEqual.ShouldBeTrue();
    }

    [Fact]
    public void GeneratedTypeEntry_OutputGraphType_EqualityComparison()
    {
        // Arrange - Create first fully populated GeneratedTypeEntry with OutputGraphType
        var entry1 = new GeneratedTypeEntry(
            SchemaClass: null,
            OutputGraphType: new OutputGraphTypeData(
                IsInterface: false,
                FullyQualifiedClrTypeName: "global::Sample.Person",
                GraphTypeClassName: "AutoOutputGraphType_Person",
                SelectedMembers: new ImmutableEquatableArray<OutputMemberData>(new[]
                {
                    new OutputMemberData(
                        DeclaringTypeFullyQualifiedName: "global::Sample.Person",
                        MemberName: "Name",
                        MemberKind: MemberKind.Property,
                        IsStatic: false,
                        IsSourceStreamResolver: false,
                        MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                        {
                            new MethodParameterData("global::System.String"),
                            new MethodParameterData("global::System.Int32")
                        })),
                    new OutputMemberData(
                        DeclaringTypeFullyQualifiedName: "global::Sample.Person",
                        MemberName: "GetFriends",
                        MemberKind: MemberKind.Method,
                        IsStatic: true,
                        IsSourceStreamResolver: true,
                        MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                        {
                            new MethodParameterData("global::Sample.DataContext"),
                            new MethodParameterData("global::System.Boolean")
                        }))
                }),
                InstanceSource: Analyzers.SourceGenerators.Models.InstanceSource.GetRequiredService,
                ConstructorData: new ConstructorData(
                    Parameters: new ImmutableEquatableArray<ConstructorParameterData>(new[]
                    {
                        new ConstructorParameterData("global::System.String"),
                        new ConstructorParameterData("global::System.Int32")
                    }),
                    RequiredProperties: new ImmutableEquatableArray<RequiredPropertyData>(new[]
                    {
                        new RequiredPropertyData("Email", "global::System.String"),
                        new RequiredPropertyData("Age", "global::System.Int32")
                    }))),
            InputGraphType: null,
            Namespace: "Sample.GraphQL",
            PartialClassHierarchy: new ImmutableEquatableArray<PartialClassInfo>(new[]
            {
                new PartialClassInfo("SampleSchema", ClassAccessibility.Public),
                new PartialClassInfo("NestedClass", ClassAccessibility.Internal)
            }));

        // Arrange - Create second identical GeneratedTypeEntry without reusing nested classes
        var entry2 = new GeneratedTypeEntry(
            SchemaClass: null,
            OutputGraphType: new OutputGraphTypeData(
                IsInterface: false,
                FullyQualifiedClrTypeName: "global::Sample.Person",
                GraphTypeClassName: "AutoOutputGraphType_Person",
                SelectedMembers: new ImmutableEquatableArray<OutputMemberData>(new[]
                {
                    new OutputMemberData(
                        DeclaringTypeFullyQualifiedName: "global::Sample.Person",
                        MemberName: "Name",
                        MemberKind: MemberKind.Property,
                        IsStatic: false,
                        IsSourceStreamResolver: false,
                        MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                        {
                            new MethodParameterData("global::System.String"),
                            new MethodParameterData("global::System.Int32")
                        })),
                    new OutputMemberData(
                        DeclaringTypeFullyQualifiedName: "global::Sample.Person",
                        MemberName: "GetFriends",
                        MemberKind: MemberKind.Method,
                        IsStatic: true,
                        IsSourceStreamResolver: true,
                        MethodParameters: new ImmutableEquatableArray<MethodParameterData>(new[]
                        {
                            new MethodParameterData("global::Sample.DataContext"),
                            new MethodParameterData("global::System.Boolean")
                        }))
                }),
                InstanceSource: Analyzers.SourceGenerators.Models.InstanceSource.GetRequiredService,
                ConstructorData: new ConstructorData(
                    Parameters: new ImmutableEquatableArray<ConstructorParameterData>(new[]
                    {
                        new ConstructorParameterData("global::System.String"),
                        new ConstructorParameterData("global::System.Int32")
                    }),
                    RequiredProperties: new ImmutableEquatableArray<RequiredPropertyData>(new[]
                    {
                        new RequiredPropertyData("Email", "global::System.String"),
                        new RequiredPropertyData("Age", "global::System.Int32")
                    }))),
            InputGraphType: null,
            Namespace: "Sample.GraphQL",
            PartialClassHierarchy: new ImmutableEquatableArray<PartialClassInfo>(new[]
            {
                new PartialClassInfo("SampleSchema", ClassAccessibility.Public),
                new PartialClassInfo("NestedClass", ClassAccessibility.Internal)
            }));

        // Act
        var areEqual = EqualityComparer<GeneratedTypeEntry>.Default.Equals(entry1, entry2);

        // Assert
        areEqual.ShouldBeTrue();
    }

    [Fact]
    public void GeneratedTypeEntry_InputGraphType_EqualityComparison()
    {
        // Arrange - Create first fully populated GeneratedTypeEntry with InputGraphType
        var entry1 = new GeneratedTypeEntry(
            SchemaClass: null,
            OutputGraphType: null,
            InputGraphType: new InputGraphTypeData(
                FullyQualifiedClrTypeName: "global::Sample.PersonInput",
                GraphTypeClassName: "AutoInputGraphType_PersonInput",
                Members: new ImmutableEquatableArray<InputMemberData>(new[]
                {
                    new InputMemberData(
                        DeclaringTypeFullyQualifiedName: "global::Sample.PersonInput",
                        MemberName: "Name",
                        FullyQualifiedTypeName: "global::System.String"),
                    new InputMemberData(
                        DeclaringTypeFullyQualifiedName: "global::Sample.PersonInput",
                        MemberName: "Age",
                        FullyQualifiedTypeName: "global::System.Int32")
                }),
                ConstructorParameters: new ImmutableEquatableArray<InputConstructorParameterData>(new[]
                {
                    new InputConstructorParameterData("Name"),
                    new InputConstructorParameterData("Age")
                })),
            Namespace: "Sample.GraphQL",
            PartialClassHierarchy: new ImmutableEquatableArray<PartialClassInfo>(new[]
            {
                new PartialClassInfo("SampleSchema", ClassAccessibility.Public),
                new PartialClassInfo("NestedClass", ClassAccessibility.Internal)
            }));

        // Arrange - Create second identical GeneratedTypeEntry without reusing nested classes
        var entry2 = new GeneratedTypeEntry(
            SchemaClass: null,
            OutputGraphType: null,
            InputGraphType: new InputGraphTypeData(
                FullyQualifiedClrTypeName: "global::Sample.PersonInput",
                GraphTypeClassName: "AutoInputGraphType_PersonInput",
                Members: new ImmutableEquatableArray<InputMemberData>(new[]
                {
                    new InputMemberData(
                        DeclaringTypeFullyQualifiedName: "global::Sample.PersonInput",
                        MemberName: "Name",
                        FullyQualifiedTypeName: "global::System.String"),
                    new InputMemberData(
                        DeclaringTypeFullyQualifiedName: "global::Sample.PersonInput",
                        MemberName: "Age",
                        FullyQualifiedTypeName: "global::System.Int32")
                }),
                ConstructorParameters: new ImmutableEquatableArray<InputConstructorParameterData>(new[]
                {
                    new InputConstructorParameterData("Name"),
                    new InputConstructorParameterData("Age")
                })),
            Namespace: "Sample.GraphQL",
            PartialClassHierarchy: new ImmutableEquatableArray<PartialClassInfo>(new[]
            {
                new PartialClassInfo("SampleSchema", ClassAccessibility.Public),
                new PartialClassInfo("NestedClass", ClassAccessibility.Internal)
            }));

        // Act
        var areEqual = EqualityComparer<GeneratedTypeEntry>.Default.Equals(entry1, entry2);

        // Assert
        areEqual.ShouldBeTrue();
    }
}
