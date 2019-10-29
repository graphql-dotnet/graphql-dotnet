using GraphQL.StarWars.Extensions;
using GraphQL.Types;
using GraphQL.DI;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL.StarWars.Types
{
    //demonstration of mixing declarative graph code and DI graph code

    public class DroidTypeDIGraph : DIObjectGraphType<DroidTypeDI, Droid>
    {
        public DroidTypeDIGraph(StarWarsData data)
        {
            Field(d => d.Id).Description("The id of the droid.");
            Field(d => d.Name, nullable: true).Description("The name of the droid.");

            Connection<CharacterInterface>()
                .Name("friendsConnection")
                .Description("A list of a character's friends.")
                .Bidirectional()
                .Resolve(context =>
                {
                    return context.GetPagedResults<Droid, StarWarsCharacter>(data, context.Source.Friends);
                });

            Interface<CharacterInterface>();
        }
    }

    [Name("Droid")]
    [Description("A mechanical creature in the Star Wars universe.")]
    public class DroidTypeDI : DIObjectGraphBase<Droid>
    {
        private StarWarsData _data;

        public DroidTypeDI(StarWarsData data)
        {
            _data = data;
        }

        [GraphType(typeof(ListGraphType<CharacterInterface>))]
        public IEnumerable<StarWarsCharacter> Friends([FromSource] Droid droid) => _data.GetFriends(droid);

        [GraphType(typeof(ListGraphType<EpisodeEnum>))]
        [Description("Which movie they appear in.")]
        public static int[] AppearsIn([FromSource] Droid droid) => droid.AppearsIn;

        [Description("The primary function of the droid.")]
        public static string PrimaryFunction([FromSource] Droid droid) => droid.PrimaryFunction;
    }
}
