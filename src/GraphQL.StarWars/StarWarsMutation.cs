using GraphQL.MicrosoftDI;
using GraphQL.StarWars.DataRepository;
using GraphQL.StarWars.Types;
using GraphQL.Types;

namespace GraphQL.StarWars
{
    /// <summary>
    /// <example>
    /// This is an example JSON request for a mutation
    /// {
    ///   "query": "mutation ($human:HumanInput!){ createHuman(human: $human) { id name } }",
    ///   "variables": {
    ///     "human": {
    ///       "name": "Boba Fett"
    ///     }
    ///   }
    /// }
    /// </example>
    /// </summary>
    public class StarWarsMutation : ObjectGraphType<object>
    {
        public StarWarsMutation()
        {
            Name = "Mutation";

            Field<HumanType>()
                .Name("createHuman")
                .Argument<NonNullGraphType<HumanInputType>>("human")
                .Resolve()
                .WithScope()
                .WithService<IStarWarsDataRespository>()
                .Resolve((context, starWarsDataRespository) => starWarsDataRespository.AddCharacter(context.GetArgument<Human>("human")));

            //Field<HumanType>(
            //    "createHuman",
            //    arguments: new QueryArguments(
            //        new QueryArgument<NonNullGraphType<HumanInputType>> { Name = "human" }
            //    ),
            //    resolve: context =>
            //    {
            //        var human = context.GetArgument<Human>("human");
            //        return data.AddCharacter(human);
            //    });
        }
    }
}
