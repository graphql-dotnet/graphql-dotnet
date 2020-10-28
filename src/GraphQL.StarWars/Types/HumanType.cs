using GraphQL.StarWars.Extensions;
using GraphQL.Types;

namespace GraphQL.StarWars.Types
{
    public class HumanType : ObjectGraphType<Human>
    {
        public HumanType(StarWarsData data)
        {
            Name = "Human";

            Field(h => h.Id).Description("The id of the human.");
            Field(h => h.Name, nullable: true).Description("The name of the human.");

            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: context => data.GetFriends(context.Source)
            );

            Connection<CharacterInterface>()
                .Name("friendsConnection")
                .Description("A list of a character's friends.")
                .Bidirectional()
                .Resolve(context => context.GetPagedResults<Human, StarWarsCharacter>(data, context.Source.Friends));

            Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");

            Field(h => h.HomePlanet, nullable: true).Description("The home planet of the human.");

            Interface<CharacterInterface>();
        }
    }
}
