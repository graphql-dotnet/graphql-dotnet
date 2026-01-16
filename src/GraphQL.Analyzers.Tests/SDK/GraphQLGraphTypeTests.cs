using GraphQL.Analyzers.SDK;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Tests.SDK;

public class GraphQLGraphTypeTests
{
    [Fact]
    public async Task TryCreate_NotGraphType_ReturnsNull()
    {
        var context = await TestContext.CreateAsync(
            """
            namespace Sample;

            public class MyClass
            {
                public MyClass()
                {
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldBeNull();
    }

    [Fact]
    public async Task TryCreate_ObjectGraphType_ReturnsInstance()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.Name.ShouldBe("MyType");
        graphType.Syntax.ShouldBe(classDeclaration);
        graphType.SemanticModel.ShouldBe(context.SemanticModel);
    }

    [Fact]
    public async Task IsInputType_InputObjectGraphType_ReturnsTrue()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyInputType : InputObjectGraphType
            {
                public MyInputType()
                {
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.IsInputType.ShouldBeTrue();
        graphType.IsOutputType.ShouldBeFalse();
    }

    [Fact]
    public async Task IsOutputType_ObjectGraphType_ReturnsTrue()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.IsOutputType.ShouldBeTrue();
        graphType.IsInputType.ShouldBeFalse();
    }

    [Fact]
    public async Task SourceType_GenericGraphType_ReturnsSourceType()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; }
            }

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    Field(x => x.Name);
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "PersonGraphType");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.SourceType.ShouldNotBeNull();
        graphType.SourceType.Name.ShouldBe("Person");
        graphType.SourceType.TypeSymbol.ShouldNotBeNull();
        graphType.SourceType.TypeSymbol.Name.ShouldBe("Person");
    }

    [Fact]
    public async Task Fields_MultipleFields_ReturnsAllFields()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public int Age { get; set; }
            }

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    Field(x => x.FirstName);
                    Field(x => x.LastName);
                    Field<IntGraphType>("age");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "PersonGraphType");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(3);
        graphType.Fields[0].Name?.Value.ShouldBe("FirstName");
        graphType.Fields[1].Name?.Value.ShouldBe("LastName");
        graphType.Fields[2].Name?.Value.ShouldBe("age");
    }

    [Fact]
    public async Task GetField_ExistingField_ReturnsField()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("firstName");
                    Field<StringGraphType>("lastName");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();

        var field = graphType.GetField("firstName");
        field.ShouldNotBeNull();
        field.Name?.Value.ShouldBe("firstName");
    }

    [Fact]
    public async Task GetField_NonExistingField_ReturnsNull()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("firstName");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();

        var field = graphType.GetField("nonExisting");
        field.ShouldBeNull();
    }

    [Fact]
    public async Task HasField_ExistingField_ReturnsTrue()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("firstName");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.HasField("firstName").ShouldBeTrue();
        graphType.HasField("nonExisting").ShouldBeFalse();
    }

    [Fact]
    public async Task TypeSymbol_ValidGraphType_ReturnsTypeSymbol()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.TypeSymbol.ShouldNotBeNull();
        graphType.TypeSymbol.Name.ShouldBe("MyType");
    }

    [Fact]
    public async Task Location_ValidGraphType_ReturnsClassLocation()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.Location.ShouldNotBeNull();

        // Verify location points to class identifier
        var sourceText = await context.SyntaxTree.GetTextAsync();
        var locationText = sourceText.ToString(graphType.Location.SourceSpan);
        locationText.ShouldBe("MyType");
    }

    [Fact]
    public async Task SourceType_Members_ReturnsPropertiesAndFields()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public int Age { get; private set; }
                public string Address;
                private string _ssn;
            }

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    Field(x => x.FirstName);
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "PersonGraphType");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.SourceType.ShouldNotBeNull();
        graphType.SourceType.Members.Count.ShouldBe(4); // Excludes private _ssn

        // Verify properties
        var firstNameMember = graphType.SourceType.GetMember("FirstName");
        firstNameMember.ShouldNotBeNull();
        firstNameMember.IsProperty.ShouldBeTrue();
        firstNameMember.IsField.ShouldBeFalse();
        firstNameMember.IsReadable.ShouldBeTrue();
        firstNameMember.IsWritable.ShouldBeTrue();
        firstNameMember.Accessibility.ShouldBe(Microsoft.CodeAnalysis.Accessibility.Public);

        var ageMember = graphType.SourceType.GetMember("Age");
        ageMember.ShouldNotBeNull();
        ageMember.IsProperty.ShouldBeTrue();
        ageMember.IsReadable.ShouldBeTrue();
        ageMember.IsWritable.ShouldBeTrue(); // private setter still makes it writable
        ageMember.Accessibility.ShouldBe(Microsoft.CodeAnalysis.Accessibility.Public);

        // Verify fields
        var addressMember = graphType.SourceType.GetMember("Address");
        addressMember.ShouldNotBeNull();
        addressMember.IsField.ShouldBeTrue();
        addressMember.IsProperty.ShouldBeFalse();
        addressMember.IsReadable.ShouldBeTrue();
        addressMember.IsWritable.ShouldBeTrue();
        addressMember.Accessibility.ShouldBe(Microsoft.CodeAnalysis.Accessibility.Public);

        // Private member should not be included
        var ssnMember = graphType.SourceType.GetMember("_ssn");
        ssnMember.ShouldBeNull();
    }

    [Fact]
    public async Task SourceType_ReadOnlyField_IsNotWritable()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public readonly string Id;

                public Person(string id)
                {
                    Id = id;
                }
            }

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    Field(x => x.Id);
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "PersonGraphType");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.SourceType.ShouldNotBeNull();

        var idMember = graphType.SourceType.GetMember("Id");
        idMember.ShouldNotBeNull();
        idMember.IsField.ShouldBeTrue();
        idMember.IsReadable.ShouldBeTrue();
        idMember.IsWritable.ShouldBeFalse();
    }

    [Fact]
    public async Task SourceType_GetOnlyProperty_IsNotWritable()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string FullName { get; }

                public Person(string fullName)
                {
                    FullName = fullName;
                }
            }

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    Field(x => x.FullName);
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "PersonGraphType");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.SourceType.ShouldNotBeNull();

        var fullNameMember = graphType.SourceType.GetMember("FullName");
        fullNameMember.ShouldNotBeNull();
        fullNameMember.IsProperty.ShouldBeTrue();
        fullNameMember.IsReadable.ShouldBeTrue();
        fullNameMember.IsWritable.ShouldBeFalse();
    }

    [Fact]
    public async Task SourceType_HasMember_ReturnsTrueForExistingMember()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; }
            }

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    Field(x => x.Name);
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "PersonGraphType");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.SourceType.ShouldNotBeNull();
        graphType.SourceType.HasMember("Name").ShouldBeTrue();
        graphType.SourceType.HasMember("NonExisting").ShouldBeFalse();
    }

    [Fact]
    public async Task SourceType_MemberType_ReturnsCorrectTypeSymbol()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; }
                public int Age { get; set; }
            }

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    Field(x => x.Name);
                    Field(x => x.Age);
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "PersonGraphType");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.SourceType.ShouldNotBeNull();

        var nameMember = graphType.SourceType.GetMember("Name");
        nameMember.ShouldNotBeNull();
        nameMember.Type.ShouldNotBeNull();
        nameMember.Type.Name.ShouldBe("String");

        var ageMember = graphType.SourceType.GetMember("Age");
        ageMember.ShouldNotBeNull();
        ageMember.Type.ShouldNotBeNull();
        ageMember.Type.Name.ShouldBe("Int32");
    }

    [Fact]
    public async Task SourceType_NoGenericParameter_ReturnsObjectType()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.SourceType.ShouldNotBeNull();
        // ObjectGraphType without generic inherits from ObjectGraphType<object?>
        graphType.SourceType.Name.ShouldBe("Object");
    }

    [Fact]
    public async Task SourceType_Location_PointsToTypeArgumentInDeclaration()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; }
            }

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    Field(x => x.Name);
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "PersonGraphType");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.SourceType.ShouldNotBeNull();
        graphType.SourceType.Location.ShouldNotBeNull();
        graphType.SourceType.Location.ShouldNotBe(Microsoft.CodeAnalysis.Location.None);

        // Verify location points to the Person type argument in ObjectGraphType<Person>
        var sourceText = await context.SyntaxTree.GetTextAsync();
        var locationText = sourceText.ToString(graphType.SourceType.Location.SourceSpan);
        locationText.ShouldBe("Person");
    }

    [Theory]
    [InlineData("public string TestMember { get; set; }", true, true, true, true)]
    [InlineData("public string TestMember;", true, false, true, true)]
    [InlineData("internal string TestMember { get; set; }", true, true, true, true)]
    [InlineData("internal string TestMember;", true, false, true, true)]
    [InlineData("protected string TestMember { get; set; }", false, true, false, false)]
    [InlineData("private string TestMember { get; set; }", false, true, false, false)]
    [InlineData("private string TestMember;", false, false, false, false)]
    [InlineData("public string TestMember { get; private set; }", true, true, true, true)]
    [InlineData("public string TestMember { get; internal set; }", true, true, true, true)]
    [InlineData("public string TestMember { get; protected set; }", true, true, true, true)]
    [InlineData("public string TestMember { private get; set; }", false, true, false, false)]
    [InlineData("public string TestMember { internal get; set; }", true, true, true, true)]
    [InlineData("internal string TestMember { get; private set; }", true, true, true, true)]
    [InlineData("public string TestMember { get; }", true, true, true, false)]
    [InlineData("private string TestMember { get; }", false, true, false, false)]
    [InlineData("public string TestMember { get; init; }", true, true, true, true)]
    [InlineData("public readonly string TestMember;", true, false, true, false)]
    public async Task SourceType_IncludesPublicAndInternalMembers(string memberDeclaration, bool shouldExist, bool isProperty, bool isReadable, bool isWritable)
    {
        var context = await TestContext.CreateAsync(
            $$"""
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                {{memberDeclaration}}
            }

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "PersonGraphType");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.SourceType.ShouldNotBeNull();

        var member = graphType.SourceType.GetMember("TestMember");

        if (shouldExist)
        {
            member.ShouldNotBeNull($"Member should exist for: {memberDeclaration}");
            member.IsProperty.ShouldBe(isProperty, $"IsProperty mismatch for: {memberDeclaration}");
            member.IsField.ShouldBe(!isProperty, $"IsField mismatch for: {memberDeclaration}");
            member.IsReadable.ShouldBe(isReadable, $"IsReadable mismatch for: {memberDeclaration}");
            member.IsWritable.ShouldBe(isWritable, $"IsWritable mismatch for: {memberDeclaration}");
        }
        else
        {
            member.ShouldBeNull($"Member should not exist due to accessibility: {memberDeclaration}");
        }
    }
}
