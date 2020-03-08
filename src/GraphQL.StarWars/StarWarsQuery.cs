using System;
using GraphQL.StarWars.Types;
using GraphQL.Types;

namespace GraphQL.StarWars
{
    public class StarWarsQuery : ObjectGraphType<object>
    {
        public StarWarsQuery(StarWarsData data
            ,Func<ScopedDependency> scopedDep // Func<T> dependency is a working alternative to T in case of singletone parent object
            , Func<ScopedOtherDependency> scopedOther
            , Defer<ScopedDependency> scopedDepDef
            , Defer<ScopedOtherDependency> scopedOtherDef
            // ,ScopedDependency invalid // uncomment this to see error "Cannot consume scoped service from singleton"
            )
        {
            Name = "Query";

            Field<StringGraphType>("scopedtest",
                resolve: context =>
                {
                    // you wil see 1111|11111, 2222|22222, 3333|33333 and so on

                    var a = scopedDep().Index;
                    var b = scopedDep().Index;

                    var c = scopedOther().Index;
                    var d = scopedOther().Index;
                    var e = scopedOther().Index;

                    var f = scopedDepDef.Value.Index;
                    var g = scopedOtherDef.Value.Index;
                    var h = scopedDepDef.Value.Index;
                    var i = scopedOtherDef.Value.Index;

                    bool same = a == b && a == f && a == h;
                    if (!same)
                        throw new ApplicationException();

                    return a.ToString() + b.ToString() + f.ToString() + h.ToString() + "|" + c.ToString() + d.ToString() + e.ToString() + g.ToString() + i.ToString();
                });
            Field<CharacterInterface>("hero", resolve: context => data.GetDroidByIdAsync("3"));
            Field<HumanType>(
                "human",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the human" }
                ),
                resolve: context => data.GetHumanByIdAsync(context.GetArgument<string>("id"))
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
