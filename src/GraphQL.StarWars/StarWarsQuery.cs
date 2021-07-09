using System;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.MicrosoftDI;
using GraphQL.StarWars.DataRepository;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.StarWars
{
    public class StarWarsQuery : ObjectGraphType<object>
    {
        //public StarWarsQuery(StarWarsData data)
        //{
        //    Name = "Query";

        //    Field<CharacterInterface>("hero", resolve: context => data.GetDroidByIdAsync("3"));

        //    Field<HumanType>(
        //        "human",
        //        arguments: new QueryArguments(
        //            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the human" }
        //        ),
        //        resolve: (context) => data.GetHumanByIdAsync(context.GetArgument<string>("id"))
        //    );

        //    Func<IResolveFieldContext, string, object> func = (context, id) => data.GetDroidByIdAsync(id);

        //    FieldDelegate<DroidType>(
        //        "droid",
        //        arguments: new QueryArguments(
        //            new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the droid" }
        //        ),
        //        resolve: func
        //    );
        //}

        public StarWarsQuery()
        {
            Name = "Query";

            Field<CharacterInterface>()
                .Name("hero")
                .Resolve()
                .WithScope()
                .WithService<IStarWarsDataRespository>()
                .ResolveAsync(async (context, starWarsDataRespository) => await starWarsDataRespository.GetDroidByIdAsync("3"));

            Field<HumanType>()
                .Name("human")
                .Argument<NonNullGraphType<StringGraphType>>("id", "id of the human")
                .Resolve()
                .WithScope()
                .WithService<IStarWarsDataRespository>()
                .ResolveAsync(async (context, starWarsDataRespository) => await starWarsDataRespository.GetHumanByIdAsync(context.GetArgument<string>("id")));

            Func<IResolveFieldContext, string, object> func = (context, id) => context.RequestServices.GetRequiredService<IStarWarsDataRespository>().GetDroidByIdAsync(id);

            FieldDelegate<DroidType>(
                "droid",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the droid" }
                ),
                resolve: func
            );
        }
    }
}
