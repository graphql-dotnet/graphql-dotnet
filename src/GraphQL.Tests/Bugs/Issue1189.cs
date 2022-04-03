using GraphQL.Tests.Utilities;
using GraphQLParser;

namespace GraphQL.Tests.Bugs;

public class Issue1189 : SchemaBuilderTestBase
{
    private const string _typeDefinitions = @"
                  type Droid {
                    id: String!
                    name: String!
                    friend: Character
                  }

                  type Character {
                    name: String!
                  }

                  type Query {
                    hero: Droid
                  }
                ";

    private const string _query = "{ hero { id name friend { name } } }";

    [Theory]
    [InlineData(typeof(Issue1189_DroidType_ExecutionError), "Error Message", null)]
    [InlineData(typeof(Issue1189_DroidType_Exception), "Error trying to resolve field 'friend'.", "")]
    public void Issue1189_Should_Work(Type resolverType, string errorMessage, string code)
    {
        Builder.Types.Include<Issue1189_Query>();
        Builder.Types.For("Character").Type = typeof(Issue1189_Character);
        Builder.Types.Include(resolverType);

        var schema = Builder.Build(_typeDefinitions);
        schema.Initialize();

        var error = new ExecutionError(errorMessage);
        error.AddLocation(new Location(1, 18));
        error.Path = new string[] { "hero", "friend" };
        error.Code = code;

        var queryResult = new ExecutionResult
        {
            Executed = true,
            Data = new { hero = new { id = "1", name = "R2-D2", friend = default(Issue1189_Character) } },
            Errors = new ExecutionErrors { error }
        };

        AssertQuery(
            _ =>
            {
                _.Schema = schema;
                _.Query = _query;
                _.ThrowOnUnhandledException = false;
            },
            queryResult);
    }
}

public class Issue1189_Droid
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class Issue1189_Character
{
    public string Name { get; set; }
}

[GraphQLMetadata("Query", IsTypeOf = typeof(Issue1189_Query))]
public class Issue1189_Query
{
    [GraphQLMetadata("hero")]
    public Issue1189_Droid GetHero()
        => new Issue1189_Droid { Id = "1", Name = "R2-D2" };
}

[GraphQLMetadata("Droid", IsTypeOf = typeof(Issue1189_Droid))]
public class Issue1189_DroidType_ExecutionError
{
    public string Id([FromSource] Issue1189_Droid droid) => droid.Id;
    public string Name([FromSource] Issue1189_Droid droid) => droid.Name;

    public Issue1189_Character Friend()
        => throw new ExecutionError("Error Message");
}

[GraphQLMetadata("Droid", IsTypeOf = typeof(Issue1189_Droid))]
public class Issue1189_DroidType_Exception
{
    public string Id([FromSource] Issue1189_Droid droid) => droid.Id;
    public string Name([FromSource] Issue1189_Droid droid) => droid.Name;

    public Issue1189_Character Friend()
        => throw new Exception("Error Message");
}
