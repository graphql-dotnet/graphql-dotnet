# Schema-First Approach Sample

This sample demonstrates the **Schema-First** approach in GraphQL.NET, where the schema is defined using the Schema Definition Language (SDL) and resolvers are configured separately in C# code.

## What is Schema-First?

In the Schema-First approach:

1. **Define the schema in SDL** (`.gql` or `.graphql` file) - This defines the types, fields, and relationships.
2. **Load the schema in C#** - Use `SchemaBuilder.CreateFrom(schemaString)` to load the SDL.
3. **Configure resolvers in C#** - Use `SchemaBuilder.Types.Include<T>()` and configure field resolvers.

This is different from:
- **Code-First**: Define the schema entirely in C# using `ObjectGraphType`, `Field<>()`, etc.
- **Type-First**: Define the schema using C# types and attributes.

## Running the Sample

1. Navigate to the sample directory:
   ```bash
   cd samples/GraphQL.SchemaFirst.Sample
   ```

2. Run the sample:
   ```bash
   dotnet run
   ```

3. Open GraphiQL at `http://localhost:5000/graphiql`

## Example Queries

### Query a hero

```graphql
query {
  hero(episode: NEWHOPE) {
    id
    name
    ... on Human {
      homePlanet
    }
    ... on Droid {
      primaryFunction
    }
  }
}
```

### Query a human by ID

```graphql
query {
  human(id: "1000") {
    id
    name
    appearsIn
  }
}
```

### Create a review (mutation)

```graphql
mutation {
  createReview(
    episode: EMPIRE
    review: { stars: 5, commentary: "Excellent!" }
  ) {
    stars
    commentary
  }
}
```

## Project Structure

```
samples/GraphQL.SchemaFirst.Sample/
├── GraphQL.SchemaFirst.Sample.csproj  # Project file
├── Program.cs                          # Main entry point, schema loading
├── Query.cs                            # Query type resolvers
├── Mutation.cs                        # Mutation type resolvers
├── StarWarsData.cs                    # Sample data
├── StarWarsSchema.gql                # SDL schema definition
└── Types/                             # Type classes
    ├── CharacterInterface.cs
    ├── DroidType.cs
    ├── EpisodeEnum.cs
    ├── HumanInputType.cs
    ├── HumanType.cs
    └── StarWarsCharacter.cs
```

## Key Concepts

### 1. SDL Schema Definition

The schema is defined in `StarWarsSchema.gql`:

```graphql
type Query {
  hero(episode: Episode): Character
  human(id: String!): Human
  humans: [Human]
  droid(id: String!): Droid
  droids: [Droid]
  character(id: String!): Character
  characters: [Character]
}

type Mutation {
  createReview(episode: Episode!, review: ReviewInput!): Review
}

interface Character {
  id: String!
  name: String
  appearsIn: [Episode]
  friends: [Character]
}

type Human implements Character {
  id: String!
  name: String
  appearsIn: [Episode]
  friends: [Character]
  homePlanet: String
}

type Droid implements Character {
  id: String!
  name: String
  appearsIn: [Episode]
  friends: [Character]
  primaryFunction: String
}
```

### 2. Loading the Schema in C#

In `Program.cs`, the schema is loaded from the embedded SDL file:

```csharp
static ISchema BuildSchema(IServiceProvider serviceProvider)
{
    // Load the schema-first SDL from an embedded resource
    var filename = "GraphQL.SchemaFirst.Sample.StarWarsSchema.gql";
    var assembly = Assembly.GetExecutingAssembly();
    using var stream = assembly.GetManifestResourceStream(filename)
        ?? throw new InvalidOperationException("Could not read schema definitions from embedded resource.");
    using var reader = new StreamReader(stream);
    var schemaString = reader.ReadToEnd();

    // Build the schema from the SDL string
    var schemaBuilder = new SchemaBuilder
    {
        ServiceProvider = serviceProvider,
    };

    // Include known types for resolvers
    schemaBuilder.Types.Include<Query>();
    schemaBuilder.Types.Include<Mutation>();

    // Build the schema from the SDL
    return schemaBuilder.Build(schemaString);
}
```

### 3. Configuring Resolvers

Resolvers are configured in `Query.cs` and `Mutation.cs` using the `[GraphQLMetadata]` attribute:

```csharp
public class Query
{
    private readonly StarWarsData _data;

    public Query(StarWarsData data)
    {
        _data = data;
    }

    [GraphQLMetadata("hero")]
    public object GetHero(Episode? episode)
    {
        // Resolver logic
    }

    [GraphQLMetadata("human")]
    public Human? GetHuman(string id)
    {
        return _data.GetHumanById(id);
    }
}
```

## Benefits of Schema-First

1. **Schema as the source of truth** - The SDL file is the single source of truth for the schema.
2. **Language agnostic** - SDL is a standard, so the schema can be shared across different languages.
3. **Tooling support** - Many GraphQL tools work with SDL (e.g., GraphQL IDEs, code generators).
4. **Separation of concerns** - Schema definition is separate from resolver logic.

## Comparison with Other Approaches

| Approach | Schema Definition | Resolvers | Pros | Cons |
|----------|-------------------|-----------|------|------|
| **Schema-First** | SDL file | C# | Schema is language-agnostic, tooling support | Need to keep SDL and resolvers in sync |
| **Code-First** | C# types | C# | Type safety, refactoring support | Schema is tied to C# |
| **Type-First** | C# types + attributes | C# | Balance of type safety and flexibility | Learning curve |

## Next Steps

- Explore the [Federation samples](../federation/) for distributed GraphQL schemas.
- Check out the [Type-First sample](../type-first/) for another approach.
- Read the [main documentation](../../README.md) for more details.
