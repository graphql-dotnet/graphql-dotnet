using System.Threading.Tasks;
using GraphQL.Builders;
using GraphQL.MicrosoftDI;
using GraphQL.StarWars.DataRepository;
using GraphQL.StarWars.Extensions;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.StarWars.Types
{
    public class DroidType : ObjectGraphType<Droid>
    {
        public DroidType()
        {
            Name = "Droid";
            Description = "A mechanical creature in the Star Wars universe.";

            Field<NonNullGraphType<StringGraphType>>("id", "The id of the droid.", resolve: context => context.Source.Id);

            Field<StringGraphType>("name", "The name of the droid.", resolve: context => context.Source.Name);

            Field<ListGraphType<CharacterInterface>>()
                .Name("friends")
                .Resolve()
                .WithScope()
                .WithService<IStarWarsDataRespository>()
                .Resolve((context, starWarsDataRespository) => starWarsDataRespository.GetFriends(context.Source));

            Connection<CharacterInterface>()
                .Name("friendsConnection")
                .Description("A list of a character's friends.")
                .Bidirectional()
                .Resolve(ResolveCharactersAsync);

            Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");

            Field<StringGraphType>("primaryFunction", "The primary function of the droid.", resolve: context => context.Source.PrimaryFunction);

            Interface<CharacterInterface>();
        }

        private async Task<Connection<StarWarsCharacter>> ResolveCharactersAsync(IResolveConnectionContext<Droid> context)
        {
            var characters = await context.RequestServices.GetRequiredService<IStarWarsDataRespository>().GetAllCharactersAsync();
            var pagedResults = context.GetPagedResults<Droid, StarWarsCharacter>(characters, context.Source.Friends);

            return pagedResults;
        }
    }
}
