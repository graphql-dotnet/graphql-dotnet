using GraphQL.DataLoader.Tests.Models;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests.Types;

public class ProductType : ObjectGraphType<Product>
{
    public ProductType()
    {
        Name = "Product";

        Field(x => x.ProductId);
        Field(x => x.Name);
        Field(x => x.Price);
        Field(x => x.Description);
    }
}
