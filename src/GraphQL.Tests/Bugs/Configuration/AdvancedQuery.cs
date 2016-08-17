using GraphQL.Types;

namespace GraphQL.Tests.Bugs.Configuration
{
    public class AdvancedQuery : ObjectGraphType
    {
        public AdvancedQuery()
        {
            Name = "Query";

            Field<ResultType>(
                "enumQuery",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AdvancedEnumType>> { Name = "episode", Description = "Episode enumeration" }
                ),
                resolve: context => new Result { EpisodeSet = context.Argument<AdvancedEnum>("episode") }
            );
        }
    }
}
