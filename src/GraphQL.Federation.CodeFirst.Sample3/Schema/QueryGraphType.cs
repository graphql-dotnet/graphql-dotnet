using GraphQL.Federation.CodeFirst.Sample3.Models;
using GraphQL.Types;

namespace GraphQL.Federation.CodeFirst.Sample3.Schema;

public class QueryGraphType : ObjectGraphType
{
    public Task<IEnumerable<Review>> Reviews([FromServices] Data data)
        => data.GetReviewsAsync();
}
