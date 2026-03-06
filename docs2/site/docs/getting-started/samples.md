# Samples

This repository includes several sample projects that demonstrate various features and use cases of GraphQL.NET. These samples follow best practices and recommended coding patterns.

## DataLoader Samples

### GraphQL.DataLoader.Sample.DI

A simple sample project featuring a basic car dealership managed with SQLite. It demonstrates the use of DataLoader to load salespeople by name, including their assigned cars in a subgraph, and exposes this data as a GraphQL server with the Altair UI. The project highlights dependency injection (DI) and class-based DataLoader usage.

**Location:** [`samples/GraphQL.DataLoader.Sample.DI`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.DataLoader.Sample.DI)

**Key Features:**
- Class-based DataLoader implementation with DI
- SQLite database integration
- ASP.NET Core integration with Altair UI
- Demonstrates batching and caching patterns

### GraphQL.DataLoader.Sample.Default

Similar to the DI sample but demonstrates using DataLoader without a dependency injection container.

**Location:** [`samples/GraphQL.DataLoader.Sample.Default`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.DataLoader.Sample.Default)

**Key Features:**
- Default DataLoader implementation without DI
- SQLite database integration
- ASP.NET Core integration

## AOT (Ahead-of-Time) Compilation Samples

### GraphQL.AotCompilationSample.CodeFirst

Demonstrates how to use GraphQL.NET in a Native AOT compiled application using the code-first approach. This sample shows how to configure GraphQL.NET for environments where reflection and dynamic code generation are limited or unavailable.

**Location:** [`samples/GraphQL.AotCompilationSample.CodeFirst`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.AotCompilationSample.CodeFirst)

**Key Features:**
- Code-first schema definition compatible with AOT
- Minimal reflection usage
- Native AOT compilation support

### GraphQL.AotCompilationSample.TypeFirst

Demonstrates how to use GraphQL.NET in a Native AOT compiled application using the type-first approach.

**Location:** [`samples/GraphQL.AotCompilationSample.TypeFirst`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.AotCompilationSample.TypeFirst)

**Key Features:**
- Type-first schema definition compatible with AOT
- Native AOT compilation support
- Attribute-based schema configuration

## GraphQL Harness

### GraphQL.Harness

A comprehensive sample application that demonstrates a complete GraphQL server setup with various features. This is the most feature-rich sample in the repository and serves as a reference implementation for production scenarios.

**Location:** [`samples/GraphQL.Harness`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Harness)

**Key Features:**
- Complete ASP.NET Core integration
- Multiple authentication and authorization patterns
- Field middleware examples
- Subscription support
- GraphiQL and Altair UI integration
- Error handling patterns
- Custom scalar types
- Complex query examples

## Federation Samples

GraphQL Federation allows you to compose multiple GraphQL services into a single unified schema. These samples demonstrate different approaches to implementing federated schemas.

### GraphQL.Federation.SchemaFirst.Sample1

Demonstrates GraphQL Federation using the schema-first approach where the schema is defined using SDL (Schema Definition Language).

**Location:** [`samples/GraphQL.Federation.SchemaFirst.Sample1`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Federation.SchemaFirst.Sample1)

**Key Features:**
- Schema-first Federation v2 support
- SDL schema definition
- Entity resolvers
- Federation directives

### GraphQL.Federation.SchemaFirst.Sample2

Another schema-first Federation example demonstrating different patterns and use cases.

**Location:** [`samples/GraphQL.Federation.SchemaFirst.Sample2`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Federation.SchemaFirst.Sample2)

### GraphQL.Federation.CodeFirst.Sample3

Demonstrates GraphQL Federation using the code-first approach where the schema is defined programmatically using C# classes.

**Location:** [`samples/GraphQL.Federation.CodeFirst.Sample3`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Federation.CodeFirst.Sample3)

**Key Features:**
- Code-first Federation v2 support
- Programmatic schema definition
- Entity resolution patterns
- Federation extension methods

### GraphQL.Federation.TypeFirst.Sample4

Demonstrates GraphQL Federation using the type-first approach where the schema is inferred from C# types with attributes.

**Location:** [`samples/GraphQL.Federation.TypeFirst.Sample4`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Federation.TypeFirst.Sample4)

**Key Features:**
- Type-first Federation v2 support
- Attribute-based configuration
- Automatic schema generation from types
- Federation attributes

## Additional Resources

### GraphQL.Server.Polyfill

A polyfill for GraphQL.NET Server that allows the sample projects to use server-like extension methods without requiring the actual GraphQL.NET Server package dependency. This is an internal helper for the samples and not intended for production use.

**Location:** [`samples/GraphQL.Server.Polyfill`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Server.Polyfill)

## Running the Samples

Most samples can be run directly from Visual Studio, Visual Studio Code, or the command line:

```bash
cd samples/[sample-name]
dotnet run
```

For samples with ASP.NET Core integration, navigate to the URL displayed in the console (typically `https://localhost:5001`) to access the GraphQL endpoint and UI.

## Contributing to Samples

When contributing samples to this repository, please ensure they:

1. Follow best practices and recommended coding patterns
2. Are well-documented with clear comments
3. Include a README.md file explaining the purpose and key features
4. Are kept focused and demonstrate specific GraphQL.NET features
5. Use the GraphQL.Server.Polyfill when server-like functionality is needed
6. Are tested and verified to work with the current version of GraphQL.NET

For more examples and community-contributed samples, see the [GraphQL.NET examples repository](https://github.com/graphql-dotnet/examples).
