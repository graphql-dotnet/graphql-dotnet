using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL2
{
    public enum Episodes
    {
        NEWHOPE = 4,
        EMPIRE = 5,
        JEDI = 6
    }

    public interface ICharacter
    {
        string Id { get; set; }
        string Name { get; set; }
        string[] Friends { get; set; }
    }

    public abstract class StarWarsCharacter : ICharacter
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string[] Friends { get; set; }
        public int[] AppearsIn { get; set; }
    }

    public class Human : StarWarsCharacter
    {
        public string HomePlanet { get; set; }
    }

    public class Droid : StarWarsCharacter
    {
        public string PrimaryFunction { get; set; }
    }

    public interface IStarWarsData
    {
        Task<Droid> DroidWithIdAsync(string id);

        Human HumanWithId(string id);

        IEnumerable<ICharacter> FriendsFor(string id);

        Task<IEnumerable<ICharacter>> FriendsForAsync(string id);
    }

    public class StarWarsData : IStarWarsData
    {
        public async Task<Droid> DroidWithIdAsync(string id)
        {
            var result = await Task.Run(() => new Droid {Id = id});
            return result;
        }

        public Human HumanWithId(string id)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ICharacter> FriendsFor(string id)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<ICharacter>> FriendsForAsync(string id)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MyType : GraphQLObjectType<Human>
    {
        public MyType()
        {
            var _ = new GraphQLObjectTypeConfig<Human>();

            _.Field(x => x.Name);

            Initialize(_);
        }
    }

    public class StarWarsSchema
    {
        public GraphQLObjectType Query { get; set; }
        public GraphQLObjectType Mutation { get; set; }

        public StarWarsSchema(IStarWarsData data)
        {
            var episodeEnum = GraphQLEnumType.For<Episodes>(_ =>
            {
                _.Name = "Episode";
                _.Description = "One of the films in the Star Wars Trilogy.";
                _.Value("NEWHOPE", Episodes.NEWHOPE, "Released in 1977.");
                _.Value("EMPIRE", Episodes.EMPIRE, "Released in 1980.");
                _.Value("JEDI", Episodes.JEDI, "Released in 1983.");
            });

            var episodeEnum2 = GraphQLEnumType.For<Episodes>();

            var characterType = GraphQLInterfaceType<ICharacter>.For(_ =>
            {
                _.Field(x => x.Id);
                _.Field("name", x => x.Name);
                _.Field("friends", new GraphQLList(new GraphQLTypeReference(_.Name)));
                _.Field("appearsIn", new GraphQLList(episodeEnum));
            });

            var droidType = GraphQLObjectType<Droid>.For(_ =>
            {
                _.Field(x => x.Name);
                _.Field(
                    name: "friends",
                    type: new GraphQLList(characterType),
                    resolve: (droid, context) => data.FriendsForAsync(droid.Id));
                _.Interface(characterType);
                _.IsOfType = value => value is Droid;
            });

            var humanType = GraphQLObjectType<Human>.For(_ =>
            {
                _.Field(x => x.Name);
                _.Field(
                    name: "friends",
                    type: new GraphQLList(characterType),
                    resolve: (human, context) => data.FriendsForAsync(human.Id));
                _.Interface(characterType);
                _.IsOfType = value => value is Human;
            });

            var inputType = GraphQLInputObjectType.For(_ =>
            {
                _.Field<string>("fred");
            });

            var inputType2 = GraphQLInputObjectType.For<ICharacter>(_ =>
            {
                _.Field(x => x.Id);
            });

            var queryRoot = GraphQLObjectType.For(_ =>
            {
                _.Name = "Root";
                _.Field(
                    "hero",
                    characterType,
                    resolve: context => data.DroidWithIdAsync("3"));
                _.Field(
                    "droid",
                    new GraphQLList(droidType),
                    args: args => args.Argument<string>("id", "Id of the droid."),
                    resolve: context => data.DroidWithIdAsync(context.Argument<string>("id")));
                _.Field(f =>
                {
                    f.Name = "human";
                    f.Type = new GraphQLList(humanType);
                    f.Type = humanType;
                    f.Resolve = new FuncFieldResolver<Human>(context => data.HumanWithId(context.Argument<string>("id")));
                    f.Argument<string>("id", "Id of the human.");
                });
            });

            Query = queryRoot;
        }
    }
}
