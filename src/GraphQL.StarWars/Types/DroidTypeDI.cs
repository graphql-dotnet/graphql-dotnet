using GraphQL.StarWars.Extensions;
using GraphQL.Types;
using GraphQL.DI;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Types.Relay;
using GraphQL.Types.Relay.DataObjects;
using GraphQL.Builders;

namespace GraphQL.StarWars.Types
{
    //demonstration of mixing declarative graph code and DI graph code

    public class DroidTypeDIGraph : DIObjectGraphType<DroidTypeDI, Droid>
    {
        public DroidTypeDIGraph()
        {
            //add some fields using declarative graph code
            Field(d => d.Id).Description("The id of the droid.");
            Field(d => d.Name, nullable: true).Description("The name of the droid.");

            //interfaces are not yet supported as an attribute on the class below, but as
            //  the code is backwards compatible, we can still use this feature
            Interface<CharacterInterface>();
        }
    }

    [Name("Droid")]
    [Description("A mechanical creature in the Star Wars universe.")]
    public class DroidTypeDI : DIObjectGraphBase<Droid>
    {
        private StarWarsData _data; //this could be a scoped service

        public DroidTypeDI(StarWarsData data)
        {
            _data = data;
        }

        [GraphType(typeof(ListGraphType<CharacterInterface>))] //not required when using GraphTypeTypeRegistry
        public IEnumerable<StarWarsCharacter> Friends([FromSource] Droid droid) => _data.GetFriends(droid);

        [GraphType(typeof(ConnectionType<CharacterInterface, EdgeType<CharacterInterface>>))] //not required when using GraphTypeTypeRegistry
        [Description("A list of a character's friends.")]
        public Connection<StarWarsCharacter> FriendsConnection(IResolveFieldContext context, [FromSource] Droid droid, int? first, int? last, string after, string before)
        {
            //for convenience, a typed [FromSource] is used, rather than the ResolveFieldContext
            //a typed ResolveFieldContext<> could be used, and the typed source argument eliminated, but
            //  the ResolveConnectionContext constructor would need to accept a typed ResolveFieldContext parameter
            //also the connection arguments are listed here, although the connectionContext is created
            //  below which handles the first/last/pagesize/etc in this sample
            //it's also possible to make the DIObjectGraphType code intelligently detect a ResolveConnectionContext<>
            //  parameter and add the appropriate arguments (probably the right answer)
            var connectionContext = new ResolveConnectionContext<Droid>(context, false, 100);
            return connectionContext.GetPagedResults<Droid, StarWarsCharacter>(_data, droid.Friends);
        }

        [GraphType(typeof(ListGraphType<EpisodeEnum>))] //not required when using GraphTypeTypeRegistry
        [Description("Which movie they appear in.")]
        public static int[] AppearsIn([FromSource] Droid droid) => droid.AppearsIn;

        [Description("The primary function of the droid.")]
        public static string PrimaryFunction([FromSource] Droid droid) => droid.PrimaryFunction;
    }
}
