using GraphQL.Federation.CodeFirst.Sample3.Models;
using GraphQL.Types;

namespace GraphQL.Federation.CodeFirst.Sample3.Schema;

public class ReviewGraphType : ObjectGraphType<Review>
{
    public ReviewGraphType()
    {
        this.Key("id");
        this.ResolveReference<Review>((ctx, source) =>
        {
            var data = ctx.RequestServices!.GetRequiredService<Data>();
            return data.GetReviewByIdAsync(source.Id);
        });
        Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
        Field(x => x.Content, type: typeof(NonNullGraphType<StringGraphType>));
        Field<NonNullGraphType<ProductGraphType>>("Product")
            .Resolve(ctx => new Product { Id = ctx.Source.ProductId });
        Field<NonNullGraphType<UserGraphType>>("Author")
            .Resolve(ctx => new User { Id = ctx.Source.UserId });
    }
}
