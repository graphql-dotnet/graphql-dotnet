using GraphQL.StarWars.Types;
using GraphQL.Types;

namespace GraphQL.StarWars;

public class StarWarsQuery : ObjectGraphType<object>
{
    public StarWarsQuery(StarWarsData data)
    {
        Name = "Query";

        Field<CharacterInterface>("hero").ResolveAsync(async context => await data.GetDroidByIdAsync("3").ConfigureAwait(false));
        Field<HumanType>("human")
            .Argument<NonNullGraphType<StringGraphType>>("id", "id of the human")
            .ResolveAsync(async context => await data.GetHumanByIdAsync(context.GetArgument<string>("id")).ConfigureAwait(false));

        Func<IResolveFieldContext, string, Task<Droid>> func = (context, id) => data.GetDroidByIdAsync(id);

        Field<DroidType, string>("droid")
            .Argument<NonNullGraphType<StringGraphType>>("id", "id of the droid")
            .ResolveDelegate(func);
    }
}
