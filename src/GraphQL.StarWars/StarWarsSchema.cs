using System;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.StarWars
{
    public class StarWarsSchema : Schema
    {
        public StarWarsSchema(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<StarWarsQuery>();
            Mutation = serviceProvider.GetRequiredService<StarWarsMutation>();

            Description = "Example StarWars universe schema";
        }
    }
}
