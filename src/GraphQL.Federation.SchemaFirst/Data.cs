namespace GraphQL.Federation.SchemaFirst;

using GraphQL;

public record CaseStudy(string CaseNumber, string? Description);
public record DeprecatedProduct(string Sku, string Package, string? Reason, User? CreatedBy);
public record Product(string Id, string? Sku, string? Package, ProductVariation? Variation, ProductDimension? Dimensions, User? CreatedBy, string? Notes, List<ProductResearch> research);
public record ProductDimension(string? Size, float? Weight, string? Unit);
public record ProductResearch(CaseStudy Study, string? Outcome);
public record ProductVariation(string Id);
public record User(string Email, string Name, int? TotalProductsCreated = 1337)
{
    public int? LengthOfEmployment { get; set; }

    [GraphQLMetadata("yearsOfEmployment")]
    public int YearsOfEmployment()
    {
        if (LengthOfEmployment == null)
        {
            throw new InvalidOperationException("yearsOfEmployment should never be null - it should be populated by _entities query");
        }

        return (int)LengthOfEmployment;
    }

    [GraphQLMetadata("averageProductsCreatedPerYear")]
    public int? AverageProductsCreatedPerYear()
    {
        if (TotalProductsCreated != null && LengthOfEmployment != null)
        {
            return Convert.ToInt32((TotalProductsCreated * 1.0) / LengthOfEmployment);
        }
        return null;
    }
};

public static class Data
{
    public static ProductDimension Dimension = new ProductDimension("small", 1, "kg");
    public static IReadOnlyList<ProductResearch> ProductResearches = new List<ProductResearch>
    {
        new ProductResearch(new CaseStudy("1234", "Federation Study"), null),
        new ProductResearch(new CaseStudy("1235", "Studio Study"), null)
    };

    public static User CreatedBy = new User("support@apollographql.com", "Jane Smith");
    public static IReadOnlyList<User> Users = new List<User>
    {
        CreatedBy
    };

    private static DeprecatedProduct deprecatedProduct = new DeprecatedProduct("apollo-federation-v1", "@apollo/federation-v1", "Migrate to Federation V2", null);
    public static IReadOnlyList<DeprecatedProduct> DeprecatedProducts = new List<DeprecatedProduct>
    {
        deprecatedProduct
    };
    public static IReadOnlyList<Product> Products = new List<Product>
    {
        new Product("apollo-federation", "federation", "@apollo/federation", new ProductVariation("OSS"), Dimension, CreatedBy, null, new List<ProductResearch> { ProductResearches[0] }),
        new Product("apollo-studio", "sku", "", new ProductVariation("platform"), Dimension, CreatedBy, null, new List<ProductResearch> { ProductResearches[1] } )
    };

}
