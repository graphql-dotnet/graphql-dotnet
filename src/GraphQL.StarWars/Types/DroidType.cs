using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.StarWars.Types
{
    public class DroidType : ObjectGraphType<Droid>
    {
        public DroidType(StarWarsData data)
        {
            Name = "Droid";
            Description = "A mechanical creature in the Star Wars universe.";

            Field(d => d.Id).Description("The id of the droid.");
            Field(d => d.Name, nullable: true).Description("The name of the droid.");

            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: context => data.GetFriends(context.Source)
            );
            Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
            Field(d => d.PrimaryFunction, nullable: true).Description("The primary function of the droid.");

            Field<StringGraphType>("testing",
                arguments: new QueryArguments(
                    new QueryArgument<ListGraphType<StringGraphType>>
                    {
                        Name = "ids"
                    }),
                resolve: context =>
                {
                    var ids = context.GetArgument<List<string>>("ids");
                    return string.Join(", ", ids);
                });

            Interface<CharacterInterface>();
        }
    }
}
