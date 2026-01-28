using GraphQL.Analyzers.SourceGenerators.Generators;
using GraphQL.Analyzers.SourceGenerators.Models;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

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
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
        });

        var registeredTypes = new ImmutableEquatableArray<RegisteredGraphTypeData>(new[]
        {
            new RegisteredGraphTypeData(
                "global::GraphQL.Types.AutoRegisteringObjectGraphType<StarWarsQuery>",
                "StarWarsQuery",
                null),
            new RegisteredGraphTypeData(
                "global::GraphQL.Types.IntGraphType",
                null,
                null),
            new RegisteredGraphTypeData(
                "global::GraphQL.Types.StringGraphType",
                null,
                null)
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
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
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
}
