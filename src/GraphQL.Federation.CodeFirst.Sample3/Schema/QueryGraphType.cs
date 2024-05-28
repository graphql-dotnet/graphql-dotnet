using GraphQL.Federation.CodeFirst.Sample3.Models;
using GraphQL.Types;

namespace GraphQL.Federation.CodeFirst.Sample3.Schema;

public class QueryGraphType : ObjectGraphType
{
    public QueryGraphType()
    {
        Field<NonNullGraphType<ListGraphType<NonNullGraphType<ReviewGraphType>>>, IEnumerable<Review>>("reviews")
            .ResolveAsync(ctx =>
            {
                var data = ctx.RequestServices!.GetRequiredService<Data>();
                return data.GetReviewsAsync()!;
            });
    }
}
