using System;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.MicrosoftDI;
using GraphQL.StarWars.DataRepository;

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

        public StarWarsQuery(StarWarsData data)
        {
            Name = "Query";

            Field<CharacterInterface>()
                .Name("hero")
                .Resolve()
                .WithScope()
                .WithService<IStarWarsDataRespository>()
                .ResolveAsync((context, starWarsDataRespository) => starWarsDataRespository.GetDroidByIdAsync("3"));

            Field<HumanType>(
                "human",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the human" }
                ),
                resolve: (context) => data.GetHumanByIdAsync(context.GetArgument<string>("id"))
            );

            Func<IResolveFieldContext, string, object> func = (context, id) => data.GetDroidByIdAsync(id);

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
