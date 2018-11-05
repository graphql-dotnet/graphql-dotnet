using GraphQL.DataLoader.Tests.Models;
using GraphQL.DataLoader.Tests.Stores;
using GraphQL.Types;

namespace GraphQL.DataLoader.Tests.Types
{
    public class ProductReviewType : ObjectGraphType<ProductReview>
    {
        public ProductReviewType(IDataLoaderContextAccessor accessor, IProductsStore products, IUsersStore users)
        {
            Name = "ProductReview";

            Field(x => x.ProductReviewId);

            Field<ProductType, Product>()
                .Name("Product")
                .ResolveAsync(ctx =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<int, Product>("GetProductsById",
                        products.GetProductsByIdAsync);

                    return loader.LoadAsync(ctx.Source.ProductId);
                });

            Field<UserType, User>()
                .Name("User")
                .ResolveAsync(ctx =>
                {
                    var loader = accessor.Context.GetOrAddBatchLoader<int, User>("GetUsersById",
                        users.GetUsersByIdAsync);

                    return loader.LoadAsync(ctx.Source.UserId);
                });

            Field(x => x.Rating);
            Field(x => x.Text);
        }
    }
}
