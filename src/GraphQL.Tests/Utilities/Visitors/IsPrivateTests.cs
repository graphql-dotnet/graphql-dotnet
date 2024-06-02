using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Utilities.Visitors;

public class IsPrivateTests
{
    private readonly ISchema _schema;
    private readonly IServiceProvider _provider;
    private readonly IDocumentExecuter _executer;

    public IsPrivateTests()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<MyQuery>());
        _provider = services.BuildServiceProvider();
        _schema = _provider.GetRequiredService<ISchema>();
        _executer = _provider.GetRequiredService<IDocumentExecuter<ISchema>>();
    }

    [Fact]
    public void VerifySchema()
    {
        var sdl = _schema.Print();
        sdl.ShouldBe("""
            schema {
              query: MyQuery
            }

            type MyQuery {
              favoriteProduct: Product!
            }

            type Product implements IProduct {
              name: String!
            }

            interface IProduct {
              name: String!
            }
            """, StringCompareShould.IgnoreLineEndings);
    }

    [Fact]
    public async Task CannotRequestPrivateField()
    {
        var result = await _executer.ExecuteAsync(new()
        {
            Query = "{ hello }",
            RequestServices = _provider,
        });
        var error = result.Errors.ShouldNotBeNull().ShouldHaveSingleItem();
        error.Message.ShouldBe("Cannot query field 'hello' on type 'MyQuery'.");
    }

    [Fact]
    public async Task CannotIntrospectPrivateType()
    {
        var result = await _executer.ExecuteAsync(new()
        {
            Query = """{ __type(name: "IProduct2") { name } }""",
            RequestServices = _provider,
        });
        var resultString = new SystemTextJson.GraphQLSerializer().Serialize(result);
        resultString.ShouldBe("""{"data":{"__type":null}}""");
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Interface, Inherited = true)]
    private class PrivateAttribute : GraphQLAttribute
    {
        public override void Modify(FieldType fieldType, bool isInputType) => fieldType.IsPrivate = true;

        public override void Modify(IGraphType graphType) => graphType.IsPrivate = true;
    }

    private class MyQuery
    {
        public static Product FavoriteProduct => new Product("Test");

        [Private]
        public static string Hello => "World";
    }

    [Implements(typeof(IProduct))]
    private class Product : IProduct, IProduct2
    {
        public Product(string name)
        {
            Name = name;
        }

        public string Name { get; }

        [Private]
        public string Test => "test2";
    }

    private interface IProduct
    {
        string Name { get; }

        [Private]
        string Test { get; }
    }

    [Private]
    private interface IProduct2
    {
        string Name { get; }
    }
}
