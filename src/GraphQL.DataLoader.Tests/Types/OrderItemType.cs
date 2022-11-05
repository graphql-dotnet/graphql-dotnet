using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests.Types;

public class OrderItemType : ObjectGraphType<OrderItem>
{
    public OrderItemType(IDataLoaderContextAccessor accessor, IProductsStore products)
    {
        Name = "OrderItem";

        Field(x => x.OrderItemId);
        Field(x => x.Quantity);
        Field(x => x.UnitPrice);

        Field<ProductType, Product>()
            .Name("Product")
            .ResolveAsync(ctx =>
            {
                var loader = accessor.Context.GetOrAddBatchLoader<int, Product>("GetProductById",
                    products.GetProductsByIdAsync);

                return loader.LoadAsync(ctx.Source.ProductId);
            });
    }
}
