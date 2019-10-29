using System;
using GraphQL.DI;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.StarWars
{
    public class StarWarsSchema : Schema
    {
        public StarWarsSchema(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            //Query = serviceProvider.GetRequiredService<StarWarsQuery>();
            Query = serviceProvider.GetRequiredService<DIObjectGraphType<StarWarsQueryDI>>();
            Mutation = serviceProvider.GetRequiredService<StarWarsMutation>();
        }
    }
}
