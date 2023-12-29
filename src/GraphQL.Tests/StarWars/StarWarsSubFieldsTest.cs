using GraphQL.StarWars;
using GraphQL.StarWars.Types;
using GraphQL.Types;

namespace GraphQL.Tests.StarWars;

public class StarWarsSubFieldsTests : StarWarsTestBase
{
    public StarWarsQuery RootQuery => (StarWarsQuery)Schema.Query;

    [Fact]
    public void subfields_is_not_null_for_ListGraphType_of_ObjectGraphType()
    {
        RootQuery.Field<ListGraphType<HumanType>>("listOfHumans").Resolve(ctx =>
        {
            ctx.SubFields.ShouldNotBeNull();
            ctx.SubFields.Keys.ShouldContain("id");
            ctx.SubFields.Keys.ShouldContain("friends");
            return new List<Human>();
        });
        const string query = """
            {
              listOfHumans {
                id
                friends {
                  name
                }
              }
            }
            """;

        const string expected = """
            {
              "listOfHumans": []
            }
            """;
        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void subfields_is_not_null_for_single_ObjectGraphType()
    {
        RootQuery.Field<HumanType>("singleHuman").Resolve(ctx =>
        {
            ctx.SubFields.ShouldNotBeNull();
            ctx.SubFields.Keys.ShouldContain("id");
            ctx.SubFields.Keys.ShouldContain("friends");
            return null;
        });

        const string query = """
            {
              singleHuman {
                id
                friends {
                  name
                }
              }
            }
            """;
        const string expected = """
            {
              "singleHuman": null
            }
            """;
        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void subfields_is_not_null_for_ListGraphType_of_InterfaceGraphType()
    {
        RootQuery.Field<ListGraphType<CharacterInterface>>("listOfCharacters").Resolve(ctx =>
        {
            ctx.SubFields.ShouldNotBeNull();
            ctx.SubFields.Keys.ShouldContain("id");
            ctx.SubFields.Keys.ShouldContain("friends");
            return new List<Human>();
        });
        const string query = """
            {
              listOfCharacters {
                id
                friends {
                  name
                }
              }
            }
            """;

        const string expected = """
            {
              "listOfCharacters": []
            }
            """;
        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void subfields_is_not_null_for_single_InterfaceGraphType()
    {
        RootQuery.Field<CharacterInterface>("singleCharacter").ResolveAsync(ctx =>
       {
           ctx.SubFields.ShouldNotBeNull();
           ctx.SubFields.Keys.ShouldContain("id");
           ctx.SubFields.Keys.ShouldContain("friends");
           return Task.FromResult<object?>(null);
       });
        const string query = """
            {
              singleCharacter {
                id
                friends {
                  name
                }
              }
            }
            """;

        const string expected = """
            {
              "singleCharacter": null
            }
            """;
        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void subfields_does_not_throw_for_primitive()
    {
        RootQuery.Field<IntGraphType>("someNumber").Resolve(ctx =>
        {
            ctx.SubFields.ShouldBeNull();
            return 1;
        });

        const string query = """
            {
              someNumber
            }
            """;
        const string expected = """
            {
              "someNumber": 1
            }
            """;
        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void subfields_does_not_throw_for_list_of_primitive()
    {
        RootQuery.Field<ListGraphType<IntGraphType>>("someNumbers").Resolve(ctx =>
        {
            ctx.SubFields.ShouldBeNull();
            return new[] { 1, 2 };
        });

        const string query = """
            {
              someNumbers
            }
            """;
        const string expected = """
            {
              "someNumbers": [1,2]
            }
            """;
        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void subfields_contains_keys_from_fragment_spread_on_non_null_fields()
    {
        RootQuery.Field<NonNullGraphType<HumanType>>("luke").Resolve(context =>
        {
            context.SubFields.ShouldNotBeNull();
            context.SubFields.Keys.ShouldContain("id");
            context.SubFields.Keys.ShouldContain("name");
            return new Human { Id = "1", Name = "Luke" };
        });

        const string query = """
            query Luke {
              luke {
                ...HumanData
              }
            }

            fragment HumanData on Human {
              id
              name
            }
            """;

        const string expected = """
            {
              "luke": {
                "id": "1",
                "name": "Luke"
              }
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void subfields_contains_keys_from_inline_fragment_on_non_null_fields()
    {
        RootQuery.Field<NonNullGraphType<HumanType>>("luke").Resolve(context =>
        {
            context.SubFields.ShouldNotBeNull();
            context.SubFields.Keys.ShouldContain("id");
            context.SubFields.Keys.ShouldContain("name");
            return new Human { Id = "1", Name = "Luke" };
        });

        const string query = """
            query Luke {
              luke {
                ...on Human
                {
                  id
                  name
                }
              }
            }
            """;

        const string expected = """
            {
              "luke": {
                "id": "1",
                "name": "Luke"
              }
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void subfields_contains_keys_from_fragment_spread_on_list_fields()
    {
        RootQuery.Field<ListGraphType<HumanType>>("lukes").Resolve(context =>
        {
            context.SubFields.ShouldNotBeNull();
            context.SubFields.Keys.ShouldContain("id");
            context.SubFields.Keys.ShouldContain("name");
            return new[] { new Human { Id = "1", Name = "Luke" }, new Human { Id = "2", Name = "Luke Copy" } };
        });

        const string query = """
            query Luke {
              lukes {
                ...HumanData
              }
            }

            fragment HumanData on Human {
              id
              name
            }
            """;

        const string expected = """
            {
              "lukes": [
                {
                  "id": "1",
                  "name": "Luke"
                },
                {
                  "id": "2",
                  "name": "Luke Copy"
                }
              ]
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void subfields_contains_keys_from_inline_fragment_on_list_fields()
    {
        RootQuery.Field<ListGraphType<HumanType>>("lukes").Resolve(context =>
        {
            context.SubFields.ShouldNotBeNull();
            context.SubFields.Keys.ShouldContain("id");
            context.SubFields.Keys.ShouldContain("name");
            return new[] { new Human { Id = "1", Name = "Luke" }, new Human { Id = "2", Name = "Luke Copy" } };
        });

        const string query = """
            query Luke {
              lukes {
                ... on Human
                {
                  id
                  name
                }
              }
            }
            """;

        const string expected = """
            {
              "lukes": [
                {
                  "id": "1",
                  "name": "Luke"
                },
                {
                  "id": "2",
                  "name": "Luke Copy"
                }
              ]
            }
            """;

        AssertQuerySuccess(query, expected);
    }
}
