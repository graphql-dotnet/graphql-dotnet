using GraphQL.Analyzers.SourceGenerators.Generators;
using GraphQL.Analyzers.SourceGenerators.Models;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/// <summary>
/// Unit tests for InputGraphTypeGenerator that generates AOT input graph type code.
/// </summary>
public class InputGraphTypeGeneratorTests
{
    [Fact]
    public void GeneratesBasicInputGraphType()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
        });

        var members = new ImmutableEquatableArray<InputMemberData>(new[]
        {
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.HumanInput",
                MemberName: "Name",
                FullyQualifiedTypeName: "string"),
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::GraphQL.StarWars.TypeFirst.Types.HumanInput",
                MemberName: "HomePlanet",
                FullyQualifiedTypeName: "string?"),
        });

        var inputType = new InputGraphTypeData(
            FullyQualifiedClrTypeName: "global::GraphQL.StarWars.TypeFirst.Types.HumanInput",
            GraphTypeClassName: "AutoInputGraphType_HumanInput",
            Members: members,
            ConstructorParameters: ImmutableEquatableArray<InputConstructorParameterData>.Empty);

        // Act
        var result = InputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, inputType);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void GeneratesInputGraphTypeWithConstructorParameters()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
        });

        var members = new ImmutableEquatableArray<InputMemberData>(new[]
        {
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.PersonInput",
                MemberName: "FirstName",
                FullyQualifiedTypeName: "string"),
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.PersonInput",
                MemberName: "LastName",
                FullyQualifiedTypeName: "string"),
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.PersonInput",
                MemberName: "Email",
                FullyQualifiedTypeName: "string"),
        });

        var constructorParams = new ImmutableEquatableArray<InputConstructorParameterData>(new[]
        {
            new InputConstructorParameterData(MemberName: "FirstName"),
            new InputConstructorParameterData(MemberName: "LastName"),
        });

        var inputType = new InputGraphTypeData(
            FullyQualifiedClrTypeName: "global::Sample.PersonInput",
            GraphTypeClassName: "AutoInputGraphType_PersonInput",
            Members: members,
            ConstructorParameters: constructorParams);

        // Act
        var result = InputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, inputType);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void GeneratesEmptyInputGraphType()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
        });

        var inputType = new InputGraphTypeData(
            FullyQualifiedClrTypeName: "global::Sample.EmptyInput",
            GraphTypeClassName: "AutoInputGraphType_EmptyInput",
            Members: ImmutableEquatableArray<InputMemberData>.Empty,
            ConstructorParameters: ImmutableEquatableArray<InputConstructorParameterData>.Empty);

        // Act
        var result = InputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, inputType);

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

        var members = new ImmutableEquatableArray<InputMemberData>(new[]
        {
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.AddressInput",
                MemberName: "Street",
                FullyQualifiedTypeName: "string"),
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.AddressInput",
                MemberName: "City",
                FullyQualifiedTypeName: "string"),
        });

        var inputType = new InputGraphTypeData(
            FullyQualifiedClrTypeName: "global::Sample.AddressInput",
            GraphTypeClassName: "AutoInputGraphType_AddressInput",
            Members: members,
            ConstructorParameters: ImmutableEquatableArray<InputConstructorParameterData>.Empty);

        // Act
        var result = InputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, inputType);

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
        var result = InputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, null);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void ReturnsEmptyStringForEmptyHierarchy()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = ImmutableEquatableArray<PartialClassInfo>.Empty;
        var inputType = new InputGraphTypeData(
            FullyQualifiedClrTypeName: "global::Sample.TestInput",
            GraphTypeClassName: "AutoInputGraphType_TestInput",
            Members: ImmutableEquatableArray<InputMemberData>.Empty,
            ConstructorParameters: ImmutableEquatableArray<InputConstructorParameterData>.Empty);

        // Act
        var result = InputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, inputType);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void GeneratesComplexInputGraphType()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
        });

        var members = new ImmutableEquatableArray<InputMemberData>(new[]
        {
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.ComplexInput",
                MemberName: "Id",
                FullyQualifiedTypeName: "int"),
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.ComplexInput",
                MemberName: "Name",
                FullyQualifiedTypeName: "string"),
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.ComplexInput",
                MemberName: "Description",
                FullyQualifiedTypeName: "string?"),
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.ComplexInput",
                MemberName: "Tags",
                FullyQualifiedTypeName: "global::System.Collections.Generic.List<string>?"),
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.ComplexInput",
                MemberName: "Metadata",
                FullyQualifiedTypeName: "global::System.Collections.Generic.Dictionary<string, object>?"),
        });

        var inputType = new InputGraphTypeData(
            FullyQualifiedClrTypeName: "global::Sample.ComplexInput",
            GraphTypeClassName: "AutoInputGraphType_ComplexInput",
            Members: members,
            ConstructorParameters: ImmutableEquatableArray<InputConstructorParameterData>.Empty);

        // Act
        var result = InputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, inputType);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void GeneratesInputGraphTypeWithAllConstructorParameters()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
        });

        var members = new ImmutableEquatableArray<InputMemberData>(new[]
        {
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.ImmutableInput",
                MemberName: "Id",
                FullyQualifiedTypeName: "int"),
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.ImmutableInput",
                MemberName: "Value",
                FullyQualifiedTypeName: "string"),
        });

        var constructorParams = new ImmutableEquatableArray<InputConstructorParameterData>(new[]
        {
            new InputConstructorParameterData(MemberName: "Id"),
            new InputConstructorParameterData(MemberName: "Value"),
        });

        var inputType = new InputGraphTypeData(
            FullyQualifiedClrTypeName: "global::Sample.ImmutableInput",
            GraphTypeClassName: "AutoInputGraphType_ImmutableInput",
            Members: members,
            ConstructorParameters: constructorParams);

        // Act
        var result = InputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, inputType);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void GeneratesInputGraphTypeWithMixedDeclaringTypes()
    {
        // Arrange
        var @namespace = "AotSample";
        var partialClassHierarchy = new ImmutableEquatableArray<PartialClassInfo>(new[]
        {
            new PartialClassInfo("SampleAotSchema", IsPublic: true)
        });

        var members = new ImmutableEquatableArray<InputMemberData>(new[]
        {
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.BaseInput",
                MemberName: "BaseProperty",
                FullyQualifiedTypeName: "string"),
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: "global::Sample.DerivedInput",
                MemberName: "DerivedProperty",
                FullyQualifiedTypeName: "int"),
            new InputMemberData(
                DeclaringTypeFullyQualifiedName: null,
                MemberName: "LocalProperty",
                FullyQualifiedTypeName: "bool"),
        });

        var inputType = new InputGraphTypeData(
            FullyQualifiedClrTypeName: "global::Sample.DerivedInput",
            GraphTypeClassName: "AutoInputGraphType_DerivedInput",
            Members: members,
            ConstructorParameters: ImmutableEquatableArray<InputConstructorParameterData>.Empty);

        // Act
        var result = InputGraphTypeGenerator.Generate(@namespace, partialClassHierarchy, inputType);

        // Assert
        result.ShouldMatchApproved(o => o.NoDiff());
    }
}
