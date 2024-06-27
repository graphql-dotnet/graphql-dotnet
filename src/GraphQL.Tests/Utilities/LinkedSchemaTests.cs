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
        sb.AppendLine(sdl);
        sb.AppendLine();
        sb.AppendLine("==== Without Imported Types ====");
        sb.AppendLine();
        sdl = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase, IncludeImportedTypes = false });
        sb.AppendLine(sdl);
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
}
