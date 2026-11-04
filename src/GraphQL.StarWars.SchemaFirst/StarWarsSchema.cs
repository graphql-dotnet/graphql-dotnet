using GraphQL.StarWars.SchemaFirst.Types;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.StarWars.SchemaFirst;

public static class StarWarsSchema
{
    public static Schema Create(IServiceProvider services)
    {
        using var stream = typeof(StarWarsSchema).Assembly.GetManifestResourceStream("GraphQL.StarWars.SchemaFirst.StarWars.graphql")
            ?? throw new InvalidOperationException("Could not read the embedded Star Wars schema.");
        using var reader = new StreamReader(stream);

        return Schema.For(reader.ReadToEnd(), builder =>
        {
            builder.ServiceProvider = services;
            builder.Types.Include<StarWarsQuery>("Query");
            builder.Types.Include<StarWarsMutation>("Mutation");
            builder.Types.Include<Human>();
            builder.Types.Include<Droid>();
            builder.Types.Include<HumanInput>();
            builder.Types.Include<Episodes>("Episode");
            builder.Types.Include<Connection<StarWarsCharacter>>("CharacterConnection");
            builder.Types.Include<Edge<StarWarsCharacter>>("CharacterEdge");
            builder.Types.Include<PageInfo>();
        });
    }
}
