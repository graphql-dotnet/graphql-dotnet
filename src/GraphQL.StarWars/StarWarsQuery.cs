using GraphQL.StarWars.Types;
using GraphQL.Types;

namespace GraphQL.StarWars;

public class StarWarsQuery : ObjectGraphType<object>
{
    public StarWarsQuery(StarWarsData data)
    {
        Name = "Query";

        FieldAsync<CharacterInterface>("hero", resolve: async context => await data.GetDroidByIdAsync("3").ConfigureAwait(false));
        FieldAsync<HumanType>(
            "human",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the human" }
            ),
            resolve: async context => await data.GetHumanByIdAsync(context.GetArgument<string>("id")).ConfigureAwait(false)
        );

        Func<IResolveFieldContext, string, Task<Droid>> func = (context, id) => data.GetDroidByIdAsync(id);

        FieldDelegate<DroidType>(
            "droid",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the droid" }
            ),
            resolve: func
        );
    }
}
