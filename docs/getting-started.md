# Basics

[GraphQL.org](http://graphql.org/) is the best place to get started learning GraphQL.

Here is a "Hello World" for GraphQL .NET.

```csharp
namespace ConsoleApplication
{
    using System;
    using System.Threading.Tasks;
    using GraphQL;
    using GraphQL.Http;
    using GraphQL.Types;

    public class Program
    {
        public static void Main(string[] args)
        {
          Run();
        }

        private static async void Run()
        {
          Console.WriteLine("Hello GraphQL!");

          var schema = new Schema { Query = new StarWarsQuery() };

          var result = await new DocumentExecuter().ExecuteAsync( _ =>
          {
            _.Schema = schema;
            _.Query = @"
                query {
                  hero {
                    id
                    name
                  }
                }
              ";
          }).ConfigureAwait(false);

          var json = new DocumentWriter(indent: true).Write(result);

          Console.WriteLine(json);
        }
    }

    public class Droid
    {
      public string Id { get; set; }
      public string Name { get; set; }
    }

    public class DroidType : ObjectGraphType<Droid>
    {
      public DroidType()
      {
        Field(x => x.Id).Description("The Id of the Droid.");
        Field(x => x.Name, nullable: true).Description("The name of the Droid.");
      }
    }

    public class StarWarsQuery : ObjectGraphType
    {
      public StarWarsQuery()
      {
        Field<DroidType>(
          "hero",
          resolve: context => new Droid { Id = "1", Name = "R2-D2" }
        );
      }
    }
}
```

Output
```
Hello GraphQL!
{
  "data": {
    "hero": {
      "id": "1",
      "name": "R2-D2"
    }
  }
}
```

# GraphiQL

TODO

# Queries

TODO

# Mutations

TODO

# Interfaces

TODO

# IsTypeOf

TODO

# Unions

TODO

# ResolveType

TODO

# Variables

TODO

# Schema Generation

There is currently nothing built-in to do GraphQL Schema generation based off of existing C# classes.  There are other community projects which do so.

* [GraphQL Conventions](https://github.com/graphql-dotnet/conventions) by [Tommy Lillehagen](https://github.com/tlil87)
* [GraphQL Annotations](https://github.com/dlukez/graphql-dotnet-annotations) by [Daniel Zimmermann](https://github.com/dlukez)
