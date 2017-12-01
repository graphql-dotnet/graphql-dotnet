using GraphQL.Validation.Rules;
using Xunit;

namespace GraphQL.Tests.Validation
{
    public class KnownTypeNamesTests : ValidationTestBase<KnownTypeNames, ValidationSchema>
    {
        [Fact]
        public void known_type_names_are_valid()
        {
            ShouldPassRule(@"
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
            ");
        }

        [Fact]
        public void unknown_nonnull_type_name_is_invalid()
        {
            ShouldFailRule(_ =>{
                _.Query = @"
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
                    ";

                _.Error(Rule.UnknownTypeMessage("Abcd", null), 2, 37);
            });
        }

        [Fact]
        public void unknown_type_names_are_invalid()
        {
            ShouldFailRule(_ =>
            {
                _.Query = @"
                  query Foo($var: JumbledUpLetters) {
                    user(id: 4) {
                      name
                      pets { ... on Badger { name }, ...PetFields }
                    }
                  }
                  fragment PetFields on Peettt {
                    name
                  }
                ";
                _.Error(Rule.UnknownTypeMessage("JumbledUpLetters", null), 2, 35);
                _.Error(Rule.UnknownTypeMessage("Badger", null), 5, 37);
                _.Error(Rule.UnknownTypeMessage("Peettt", new[] {"Pet"}), 8, 41);
            });
        }
    }
}
