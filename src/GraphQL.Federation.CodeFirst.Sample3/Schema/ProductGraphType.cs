using GraphQL.Types;
using GraphQL.Federation.CodeFirst.Sample3.Models;
using GraphQL.Federation.Extensions;

namespace GraphQL.Federation.CodeFirst.Sample3.Schema;

public class ProductGraphType : ObjectGraphType<Product>
{
    public ProductGraphType()
    {
        this.Key("id");
        this.ResolveReference((_, source) => source); // Product.Id is provided through the source object and no other properties exist
        Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<ReviewGraphType>>>, IEnumerable<Review>>("reviews")
            .ResolveAsync(ctx =>
            {
                var data = ctx.RequestServices!.GetRequiredService<Data>();
                return data.GetReviewsByProductIdAsync(ctx.Source.Id)!;
            });
    }
}
