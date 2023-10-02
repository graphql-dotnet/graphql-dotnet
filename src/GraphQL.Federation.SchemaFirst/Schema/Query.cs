using GraphQL;

namespace GraphQL.Federation.SchemaFirst.Schema;

[GraphQLMetadata("Query")]
public class Query
{
    [GraphQLMetadata("product")]
    public Task<Product?> GetProductById(string Id)
    {
        return Task.FromResult(Data.Products.FirstOrDefault(p => p.Id == Id));
    }

    [GraphQLMetadata("deprecatedProduct", DeprecationReason = "Use product query instead")]
    public Task<DeprecatedProduct?> GetDeprecatedProductBySkuAndPackage(string Sku, string Package)
    {
        return Task.FromResult(Data.DeprecatedProducts.FirstOrDefault(p => p.Sku == Sku && p.Package == Package));
    }
}
