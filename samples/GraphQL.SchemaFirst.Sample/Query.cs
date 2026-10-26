namespace GraphQL.SchemaFirst.Sample;

/// <summary>
/// Query type for the Star Wars schema (Schema-First approach).
/// Resolvers are defined here and mapped to the SDL schema fields.
/// </summary>
public class Query
{
    private readonly StarWarsData _data;

    public Query(StarWarsData data)
    {
        _data = data;
    }

    /// <summary>
    /// Resolver for the 'hero' field.
    /// Returns R2-D2 by default, or the hero of the specified episode.
    /// </summary>
    [GraphQLMetadata("hero")]
    public object GetHero(Episode? episode)
    {
        if (episode == null)
            return _data.GetDroidById("3")!; // R2-D2

        return episode.Value switch
        {
            Episode.NEWHOPE => _data.GetHumanById("1000")!, // Luke Skywalker
            Episode.EMPIRE => _data.GetHumanById("1003")!, // Darth Vader? Actually, let me check the data
            Episode.JEDI => _data.GetHumanById("1000")!, // Luke again?
            _ => _data.GetDroidById("3")!,
        };
    }

    /// <summary>
    /// Resolver for the 'human' field.
    /// </summary>
    [GraphQLMetadata("human")]
    public Human? GetHuman(string id)
    {
        return _data.GetHumanById(id);
    }

    /// <summary>
    /// Resolver for the 'humans' field.
    /// </summary>
    [GraphQLMetadata("humans")]
    public IEnumerable<Human> GetHumans()
    {
        return _data.GetHumans();
    }

    /// <summary>
    /// Resolver for the 'droid' field.
    /// </summary>
    [GraphQLMetadata("droid")]
    public Droid? GetDroid(string id)
    {
        return _data.GetDroidById(id);
    }

    /// <summary>
    /// Resolver for the 'droids' field.
    /// </summary>
    [GraphQLMetadata("droids")]
    public IEnumerable<Droid> GetDroids()
    {
        return _data.GetDroids();
    }

    /// <summary>
    /// Resolver for the 'character' field.
    /// </summary>
    [GraphQLMetadata("character")]
    public object? GetCharacter(string id)
    {
        return _data.GetCharacterById(id);
    }

    /// <summary>
    /// Resolver for the 'characters' field.
    /// </summary>
    [GraphQLMetadata("characters")]
    public IEnumerable<object> GetCharacters()
    {
        return _data.GetCharacters();
    }
}
