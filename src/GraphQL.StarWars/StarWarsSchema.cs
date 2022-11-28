using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.StarWars;

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
