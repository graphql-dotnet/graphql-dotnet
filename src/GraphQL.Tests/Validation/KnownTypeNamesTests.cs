using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class KnownTypeNamesTests : ValidationTestBase<KnownTypeNames, ValidationSchema>
{
    [Fact]
    public void known_type_names_are_valid()
    {
        ShouldPassRule("""
              query Foo($var: String, $required: [String!]!) {
                user(id: 4) {
                  pets {
                    ... on Pet { name },
                    ...PetFields
                  }
                }
              }
              fragment PetFields on Pet {
                name
              }
            """,
        "{ \"required\": [\"\"] }");
    }

    [Fact]
    public void unknown_nonnull_type_name_is_invalid()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                query Foo($var: Abcd!) {
                    user(id: 4) {
                        pets {
                            ... on Pet { name },
                            ...PetFields
                        }
                    }
                }
                fragment PetFields on Pet {
                    name
                }
                """;
            _.Error(KnownTypeNamesError.UnknownTypeMessage("Abcd", null), 1, 17);
            _.Error("Variable '$var' is invalid. Variable has unknown type 'Abcd'", 1, 11);
        });
    }

    [Fact]
    public void unknown_type_names_are_invalid()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                  query Foo($var: JumbledUpLetters) {
                    user(id: 4) {
                      name
                      pets { ... on Badger { name }, ...PetFields }
                    }
                  }
                  fragment PetFields on Peettt {
                    name
                  }
                """;
            _.Error(KnownTypeNamesError.UnknownTypeMessage("JumbledUpLetters", null), 1, 19);
            _.Error(KnownTypeNamesError.UnknownTypeMessage("Badger", null), 4, 21);
            _.Error(KnownTypeNamesError.UnknownTypeMessage("Peettt", new[] { "Pet" }), 7, 25);
            _.Error("Variable '$var' is invalid. Variable has unknown type 'JumbledUpLetters'", 1, 13);
        });
    }
}
