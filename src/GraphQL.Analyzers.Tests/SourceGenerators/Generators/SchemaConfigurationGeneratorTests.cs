using GraphQL.Analyzers.SourceGenerators.Generators;
using GraphQL.Analyzers.SourceGenerators.Models;

namespace GraphQL.Analyzers.Tests.SourceGenerators.Generators;

/// <summary>
/// Unit tests for SchemaConfigurationGenerator that generates AOT schema configuration code.
/// </summary>
public class SchemaConfigurationGeneratorTests
{
    [Fact]
    public void GeneratesBasicSchemaWithConstructor()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", Accessibility: ClassAccessibility.Public)
        });

        var registeredTypes = new ImmutableEquatableArray<RegisteredGraphTypeData>(new[]
        {
            new RegisteredGraphTypeData(
                "global::GraphQL.Types.AutoRegisteringObjectGraphType<StarWarsQuery>",
                "StarWarsQuery",
                null,
                null),
            new RegisteredGraphTypeData(
                "global::GraphQL.Types.IntGraphType",
                null,
                null,
                new ConstructorData(ImmutableEquatableArray<ConstructorParameterData>.Empty, ImmutableEquatableArray<RequiredPropertyData>.Empty)),
            new RegisteredGraphTypeData(
                "global::GraphQL.Types.StringGraphType",
                null,
                null,
                new ConstructorData(ImmutableEquatableArray<ConstructorParameterData>.Empty, ImmutableEquatableArray<RequiredPropertyData>.Empty)),
            new RegisteredGraphTypeData(
                "global::GraphQL.Types.IdGraphType",
                null,
                "global::GraphQL.Types.GuidGraphType",
                new ConstructorData(ImmutableEquatableArray<ConstructorParameterData>.Empty, ImmutableEquatableArray<RequiredPropertyData>.Empty)),
        });

        var typeMappings = new ImmutableEquatableArray<TypeMappingData>(new[]
        {
            new TypeMappingData("int", "global::GraphQL.Types.IntGraphType"),
            new TypeMappingData("string", "global::GraphQL.Types.StringGraphType")
        });

        var schemaClass = new SchemaClassData(
            HasConstructor: false,
            RegisteredGraphTypes: registeredTypes,
            TypeMappings: typeMappings,
            QueryRootTypeName: "global::GraphQL.Types.AutoRegisteringObjectGraphType<StarWarsQuery>",
            MutationRootTypeName: null,
            SubscriptionRootTypeName: null,
            ArrayListTypes: new[] { new ListElementTypeData("int", false), new ListElementTypeData("int?", true), new ListElementTypeData("string?", true) }.ToImmutableEquatableArray(),
            GenericListTypes: new[] { new ListElementTypeData("int", false), new ListElementTypeData("int?", true), new ListElementTypeData("string?", true) }.ToImmutableEquatableArray(),
            HashSetTypes: new[] { new ListElementTypeData("int", false), new ListElementTypeData("int?", true), new ListElementTypeData("string?", true) }.ToImmutableEquatableArray());

        // Act
        var result = SchemaConfigurationGenerator.Generate(@namespace, partialClassHierarchy, schemaClass);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void GeneratesEmptySchemaWithNoConstructor()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", Accessibility: ClassAccessibility.Public)
        });

        var schemaClass = new SchemaClassData(
            HasConstructor: true,
            RegisteredGraphTypes: ImmutableEquatableArray<RegisteredGraphTypeData>.Empty,
            TypeMappings: ImmutableEquatableArray<TypeMappingData>.Empty,
            QueryRootTypeName: null,
            MutationRootTypeName: null,
            SubscriptionRootTypeName: null,
            ArrayListTypes: ImmutableEquatableArray<ListElementTypeData>.Empty,
            GenericListTypes: ImmutableEquatableArray<ListElementTypeData>.Empty,
            HashSetTypes: ImmutableEquatableArray<ListElementTypeData>.Empty);

        // Act
        var result = SchemaConfigurationGenerator.Generate(@namespace, partialClassHierarchy, schemaClass);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void GeneratesFactoryLambdaForTypesWithConstructorParameters()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", Accessibility: ClassAccessibility.Public)
        });

        var registeredTypes = new ImmutableEquatableArray<RegisteredGraphTypeData>(new[]
        {
            // AOT-generated type - should use standard AddAotType
            new RegisteredGraphTypeData(
                "global::GraphQL.Types.AutoRegisteringObjectGraphType<Query>",
                "QueryGraphType",
                null,
                null),
            // Type with zero params - should use standard AddAotType
            new RegisteredGraphTypeData(
                "global::GraphQL.Types.StringGraphType",
                null,
                null,
                new ConstructorData(ImmutableEquatableArray<ConstructorParameterData>.Empty, ImmutableEquatableArray<RequiredPropertyData>.Empty)),
            // Type with constructor parameter - should generate factory lambda
            new RegisteredGraphTypeData(
                "global::CustomScalarGraphType",
                null,
                null,
                new ConstructorData(
                    new[] { new ConstructorParameterData("global::System.String") }.ToImmutableEquatableArray(),
                    ImmutableEquatableArray<RequiredPropertyData>.Empty)),
            // Type with multiple constructor parameters - should generate factory lambda
            new RegisteredGraphTypeData(
                "global::ComplexScalarGraphType",
                null,
                null,
                new ConstructorData(
                    new[]
                    {
                        new ConstructorParameterData("global::System.String"),
                        new ConstructorParameterData("global::System.String"),
                        new ConstructorParameterData("global::System.IServiceProvider")
                    }.ToImmutableEquatableArray(),
                    ImmutableEquatableArray<RequiredPropertyData>.Empty)),
            // Type with required properties - should generate factory lambda with object initializer
            new RegisteredGraphTypeData(
                "global::TypeWithRequiredProps",
                null,
                null,
                new ConstructorData(
                    new[] { new ConstructorParameterData("global::System.String") }.ToImmutableEquatableArray(),
                    new[]
                    {
                        new RequiredPropertyData("Name", "global::System.String"),
                        new RequiredPropertyData("Count", "global::System.Int32")
                    }.ToImmutableEquatableArray())),
            // Type with override - should use override type in factory
            new RegisteredGraphTypeData(
                "global::GraphQL.Types.IdGraphType",
                null,
                "global::GraphQL.Types.GuidGraphType",
                new ConstructorData(ImmutableEquatableArray<ConstructorParameterData>.Empty, ImmutableEquatableArray<RequiredPropertyData>.Empty)),
            // Type with null constructor data - should not generate AddAotType
            new RegisteredGraphTypeData(
                "global::UnknownGraphType",
                null,
                null,
                null),
        });

        var typeMappings = new ImmutableEquatableArray<TypeMappingData>(new[]
        {
            new TypeMappingData("string", "global::GraphQL.Types.StringGraphType")
        });

        var schemaClass = new SchemaClassData(
            HasConstructor: false,
            RegisteredGraphTypes: registeredTypes,
            TypeMappings: typeMappings,
            QueryRootTypeName: "global::GraphQL.Types.AutoRegisteringObjectGraphType<Query>",
            MutationRootTypeName: null,
            SubscriptionRootTypeName: null,
            ArrayListTypes: ImmutableEquatableArray<ListElementTypeData>.Empty,
            GenericListTypes: ImmutableEquatableArray<ListElementTypeData>.Empty,
            HashSetTypes: ImmutableEquatableArray<ListElementTypeData>.Empty);

        // Act
        var result = SchemaConfigurationGenerator.Generate(@namespace, partialClassHierarchy, schemaClass);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }
}
