using System.Text;
using GraphQL.Types;
using GraphQL.Types.Scalars;

namespace GraphQL.Tests.Utilities;

public class LinkedSchemaTests
{
    [Theory]
    [InlineData(1, null, null, null, null)]
    [InlineData(1, "link", null, null, null)]
    [InlineData(2, "ns", null, null, null)]
    [InlineData(3, null, "Purpose", null, null)]
    [InlineData(4, null, null, "Import", null)]
    [InlineData(5, null, "LinkPurpose", null, null)]
    [InlineData(6, null, null, "LinkImport", null)]
    [InlineData(7, null, null, null, LinkPurpose.Security)]
    public void AddLinkDirectiveSupport(int i, string? defaultNamespacePrefix, string? purposeAlias, string? importAlias, LinkPurpose? purpose)
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<StringGraphType>("dummy");
        var schema = new Schema { Query = queryType };
        schema.AddLinkDirectiveSupport(c =>
        {
            if (defaultNamespacePrefix != null)
                c.Namespace = defaultNamespacePrefix;
            if (purposeAlias != null)
                c.Imports["Purpose"] = purposeAlias;
            if (importAlias != null)
                c.Imports["Import"] = importAlias;
            if (purpose != null)
                c.Purpose = purpose;
        });
        schema.Initialize();
        PrintSchema(schema).ShouldMatchApproved(o => o.NoDiff().WithDiscriminator(i.ToString()));
    }

    private string PrintSchema(ISchema schema)
    {
        var sb = new StringBuilder();
        var sdl = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase });
        sb.Append(sdl);
        sb.AppendLine();
        sb.AppendLine("==== Without Imported Types ====");
        sb.AppendLine();
        sdl = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase, IncludeImportedDefinitions = false });
        sb.Append(sdl);
        return sb.ToString();
    }

    [Theory]
    [InlineData(1, "The @link directive must be imported.")]
    [InlineData(2, "The @link directive must be imported without an alias.")]
    [InlineData(3, "The 'Test' import name is not valid; please specify only '@link', 'Purpose', and/or 'Import'.")]
    public void AddLinkDirectiveSupport_Throws(int i, string errorMessage)
    {
        var schema = new Schema();
        var message = Should.Throw<InvalidOperationException>(() => schema.AddLinkDirectiveSupport(c =>
        {
            switch (i)
            {
                case 1:
                    c.Imports.Remove("@link");
                    break;
                case 2:
                    c.Imports["@link"] = "@linkDirective";
                    break;
                case 3:
                    c.Imports.Add("Test", "Test");
                    break;
            }
        })).Message;
        message.ShouldBe(errorMessage);
    }

    [Theory]
    [InlineData(1, "https://spec.example.com/a/b/example/v1.0/", "example")]
    [InlineData(2, "https://spec.example.com/a/b/example/v1.0", "example")]
    [InlineData(3, "https://spec.example.com/a/b/example/", "example")]
    [InlineData(4, "https://spec.example.com/a/b/example", "example")]
    [InlineData(5, "https://spec.example.com/v1.0", null)]
    [InlineData(6, "https://spec.example.com/vX", "vX")]
    [InlineData(7, "https://spec.example.com/", null)]
    [InlineData(8, "https://spec.example.com", null)]
    [InlineData(null, "https://spec.example.com/a/b/ex%61mple", "example")]
    [InlineData(null, "https://spec.example.com/test/v1", "test")]
    [InlineData(null, "https://spec.example.com/test/v1.", null)]
    [InlineData(null, "https://spec.example.com/test/ab.cd/v1.0", null)]
    [InlineData(null, "https://spec.example.com/test/ab_cd/v1.0", "ab_cd")]
    [InlineData(null, "https://spec.example.com/test/ab__cd/v1.0", null)]
    [InlineData(null, "abcd", null)]
    [InlineData(null, "abcd/example/v1.0", null)]
    public void NamespaceTests(int? i, string url, string? expectedNs)
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<StringGraphType>("dummy");
        var schema = new Schema { Query = queryType };
        schema.LinkSchema(url);
        var type1 = new ObjectGraphType() { Name = (expectedNs ?? "example") + "__Type1" };
        type1.Field<StringGraphType>("field1");
        schema.RegisterType(type1);
        var type2 = new ObjectGraphType() { Name = "Type2" };
        type2.Field<StringGraphType>("field2");
        schema.RegisterType(type2);
        var directive1 = new Directive((expectedNs ?? "example") + "__Directive1");
        directive1.Locations.Add(GraphQLParser.AST.DirectiveLocation.FieldDefinition);
        schema.Directives.Register(directive1);
        var directive2 = new Directive("Directive2");
        directive2.Locations.Add(GraphQLParser.AST.DirectiveLocation.FieldDefinition);
        schema.Directives.Register(directive2);

        schema.Initialize();

        var link = schema.GetLinkedSchemas().Single(x => x.Url == url);
        link.Namespace.ShouldBe(expectedNs);

        if (i.HasValue)
        {
            PrintSchema(schema).ShouldMatchApproved(o => o.NoDiff().WithDiscriminator(i.Value.ToString()));
        }
    }

    [Fact]
    public void AllowsDoubleConfiguration()
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        queryType.Field<StringGraphType>("dummy");
        var schema = new Schema { Query = queryType };
        schema.LinkSchema("https://spec.example.com/example", c => c.Imports.Add("@key", "@key"));
        schema.LinkSchema("https://spec.example.com/example", c => c.Imports.Add("@shareable", "@share"));
        schema.Initialize();
        PrintSchema(schema).ShouldMatchApproved(o => o.NoDiff());
    }

    [Fact]
    public void AppliedDirectivesAreProperlyRenamed()
    {
        var queryType = new ObjectGraphType { Name = "Query" };
        var schema = new Schema { Query = queryType };

        schema.LinkSchema("https://spec.example.com/exampleA", c => c.Imports.Add("@importedA", "@importedA"));
        schema.Directives.Register(NewDirective("importedA"));
        schema.Directives.Register(NewDirective("exampleA__testA"));
        schema.LinkSchema("https://spec.example.com/exampleB/v1.0", c => c.Imports.Add("@importedB", "@importedB"));
        schema.Directives.Register(NewDirective("importedB"));
        schema.Directives.Register(NewDirective("failB"));
        schema.Directives.Register(NewDirective("exampleB__testB"));
        schema.LinkSchema("https://spec.example.com/exampleC", c =>
        {
            c.Imports.Add("@importedC", "@aliasC");
            c.Namespace = "customC";
        });
        schema.Directives.Register(NewDirective("aliasC"));
        schema.Directives.Register(NewDirective("customC__testC"));

        queryType.Field<StringGraphType>("field1")
            .ApplyDirective("importedA", c => c.FromSchemaUrl = "https://spec.example.com/exampleA");
        queryType.Field<StringGraphType>("field2")
            .ApplyDirective("testA", c => c.FromSchemaUrl = "https://spec.example.com/exampleA");
        queryType.Field<StringGraphType>("field3")
            .ApplyDirective("failB", c => c.FromSchemaUrl = "https://spec.example.com/exampleB");
        queryType.Field<StringGraphType>("field4")
            .ApplyDirective("importedB", c => c.FromSchemaUrl = "https://spec.example.com/exampleB/");
        queryType.Field<StringGraphType>("field5")
            .ApplyDirective("testB", c => c.FromSchemaUrl = "https://spec.example.com/exampleB/");
        queryType.Field<StringGraphType>("field6")
            .ApplyDirective("failB", c => c.FromSchemaUrl = "https://spec.example.com/exampleB/v1");
        queryType.Field<StringGraphType>("field7")
            .ApplyDirective("importedB", c => c.FromSchemaUrl = "https://spec.example.com/exampleB/v1.0");
        queryType.Field<StringGraphType>("field8")
            .ApplyDirective("testB", c => c.FromSchemaUrl = "https://spec.example.com/exampleB/v1.0");
        queryType.Field<StringGraphType>("field9")
            .ApplyDirective("failB", c => c.FromSchemaUrl = "https://spec.example.com/exampleB/v2.0");
        queryType.Field<StringGraphType>("field10")
            .ApplyDirective("importedC", c => c.FromSchemaUrl = "https://spec.example.com/exampleC");
        queryType.Field<StringGraphType>("field11")
            .ApplyDirective("testC", c => c.FromSchemaUrl = "https://spec.example.com/exampleC");

        schema.Initialize();
        PrintSchema(schema).ShouldMatchApproved(o => o.NoDiff());

        Directive NewDirective(string name)
        {
            var d = new Directive(name);
            d.Locations.Add(GraphQLParser.AST.DirectiveLocation.FieldDefinition);
            return d;
        }
    }

    [Fact]
    public void AppliedDirectivesAreProperlyRenamedForAllLocations()
    {
        var schema = new Schema();
        var url = "https://spec.example.com/example";
        schema.LinkSchema(url, c => c.Imports.Add("@test", "@testAlias"));

        // Define the custom directive @test
        var testDirective = new Directive("testAlias");
        testDirective.Locations.Add(GraphQLParser.AST.DirectiveLocation.Schema);
        testDirective.Locations.Add(GraphQLParser.AST.DirectiveLocation.Scalar);
        testDirective.Locations.Add(GraphQLParser.AST.DirectiveLocation.Object);
        testDirective.Locations.Add(GraphQLParser.AST.DirectiveLocation.FieldDefinition);
        testDirective.Locations.Add(GraphQLParser.AST.DirectiveLocation.ArgumentDefinition);
        testDirective.Locations.Add(GraphQLParser.AST.DirectiveLocation.Interface);
        testDirective.Locations.Add(GraphQLParser.AST.DirectiveLocation.Union);
        testDirective.Locations.Add(GraphQLParser.AST.DirectiveLocation.Enum);
        testDirective.Locations.Add(GraphQLParser.AST.DirectiveLocation.EnumValue);
        testDirective.Locations.Add(GraphQLParser.AST.DirectiveLocation.InputObject);
        testDirective.Locations.Add(GraphQLParser.AST.DirectiveLocation.InputFieldDefinition);
        schema.Directives.Register(testDirective);

        // Apply the @test directive to the schema
        schema.ApplyDirective("test", d => d.FromSchemaUrl = url);

        // Define scalar
        var dateScalar = new DateGraphType();
        dateScalar.ApplyDirective("test", d => d.FromSchemaUrl = url);
        schema.RegisterType(dateScalar);

        // Define User object
        var userType = new ObjectGraphType() { Name = "User" };
        userType.ApplyDirective("test", d => d.FromSchemaUrl = url);
        userType.Field<NonNullGraphType<IdGraphType>>("id").ApplyDirective("test", d => d.FromSchemaUrl = url);
        userType.Field<StringGraphType>("name").ApplyDirective("test", d => d.FromSchemaUrl = url);
        userType.Field<IntGraphType>("age");
        schema.RegisterType(userType);

        // Define Query type
        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field("getUser", userType)
            .Argument<IdGraphType>("id", c => c.ApplyDirective("test", d => d.FromSchemaUrl = url));
        schema.Query = queryType;

        // Define Node interface
        var nodeInterface = new InterfaceGraphType() { Name = "Node" };
        nodeInterface.ApplyDirective("test", d => d.FromSchemaUrl = url);
        nodeInterface.Field<NonNullGraphType<IdGraphType>>("id");
        schema.RegisterType(nodeInterface);

        // Define Post object (part of union)
        var postType = new ObjectGraphType() { Name = "Post" };
        postType.Field<StringGraphType>("title");
        schema.RegisterType(postType);

        // Define SearchResult union
        var searchResultUnion = new UnionGraphType() { Name = "SearchResult" };
        searchResultUnion.ApplyDirective("test", d => d.FromSchemaUrl = url);
        searchResultUnion.PossibleTypes.Add(userType);
        searchResultUnion.PossibleTypes.Add(postType);
        searchResultUnion.ResolveType = _ => userType;
        schema.RegisterType(searchResultUnion);

        // Define Role enum
        var roleEnum = new EnumerationGraphType() { Name = "Role" };
        roleEnum.ApplyDirective("test", d => d.FromSchemaUrl = url);
        roleEnum.Add(new EnumValueDefinition("ADMIN", 1).ApplyDirective("test", d => d.FromSchemaUrl = url));
        roleEnum.Add(new EnumValueDefinition("USER", 2));
        schema.RegisterType(roleEnum);

        // Define UserInput input object
        var userInput = new InputObjectGraphType() { Name = "UserInput" };
        userInput.ApplyDirective("test", d => d.FromSchemaUrl = url);
        userInput.Field<NonNullGraphType<StringGraphType>>("name").ApplyDirective("test", d => d.FromSchemaUrl = url);
        userInput.Field<IntGraphType>("age");
        schema.RegisterType(userInput);

        // Define Mutation type
        var mutationType = new ObjectGraphType() { Name = "Mutation" };
        mutationType.Field("createUser", userType)
            .Argument(userInput, "input", c => c.ApplyDirective("test", d => d.FromSchemaUrl = url));
        schema.Mutation = mutationType;

        schema.Initialize();
        PrintSchema(schema).ShouldMatchApproved(o => o.NoDiff());
    }
}
