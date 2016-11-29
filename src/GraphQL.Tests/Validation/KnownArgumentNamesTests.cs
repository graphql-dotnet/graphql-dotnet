using GraphQL.Validation.Rules;
using Xunit;

namespace GraphQL.Tests.Validation
{
    public class KnownArgumentNamesTests : ValidationTestBase<KnownArgumentNames, ValidationSchema>
    {
        [Fact]
        public void single_arg_is_known()
        {
            ShouldPassRule(@"
              fragment argOnRequiredArg on Dog {
                doesKnowCommand(dogCommand: SIT)
              }
            ");
        }

        [Fact]
        public void no_args_are_known()
        {
            ShouldPassRule(@"
              fragment multipleArgs on ComplicatedArgs {
                noArgsField
              }
            ");
        }

        [Fact]
        public void multiple_args_are_known()
        {
            ShouldPassRule(@"
              fragment multipleArgs on ComplicatedArgs {
                multipleReqs(req1: 1, req2: 2)
              }
            ");
        }

        [Fact]
        public void ignores_args_of_unknown_fields()
        {
            ShouldPassRule(@"
              fragment argOnUnknownField on Dog {
                unknownField(unknownArg: SIT)
              }
            ");
        }

        [Fact]
        public void multiple_args_in_reverse_order_are_known()
        {
            ShouldPassRule(@"
              fragment multipleArgsReverseOrder on ComplicatedArgs {
                multipleReqs(req2: 2, req1: 1)
              }
            ");
        }

        [Fact]
        public void no_args_on_optional_arg()
        {
            ShouldPassRule(@"
              fragment noArgOnOptionalArg on Dog {
                isHousetrained
              }
            ");
        }

        [Fact]
        public void args_are_known_deeply()
        {
            ShouldPassRule(@"
              {
                dog {
                  doesKnowCommand(dogCommand: SIT)
                }
                human {
                  pet {
                    ... on Dog {
                      doesKnowCommand(dogCommand: SIT)
                    }
                  }
                }
              }
            ");
        }

        [Fact]
        public void directive_args_are_known()
        {
            ShouldPassRule(@"
              {
                dog @skip(if: true)
              }
            ");
        }

        [Fact]
        public void field_with_no_args_given_arg_is_invalid()
        {
            ShouldFailRule(_ =>
            {
                _.Query = @"
                  fragment multipleArgs on ComplicatedArgs {
                    noArgsField(first: 1)
                  }
                ";
                _.Error(Rule.UnknownArgMessage("first", "noArgsField", "ComplicatedArgs", null), 3, 33);
            });
        }

        [Fact]
        public void undirective_args_are_invalid()
        {
            ShouldFailRule(_ =>
            {
                _.Query = @"
                  {
                    dog @skip(unless: true)
                  }
                ";
                _.Error(Rule.UnknownDirectiveArgMessage("unless", "skip", null), 3, 31);
            });
        }

        [Fact]
        public void invalid_arg_name()
        {
            ShouldFailRule(_ =>
            {
                _.Query = @"
                  fragment oneGoodArgOneInvalidArg on Dog {
                    doesKnowCommand(whoknows: 1, dogCommand: SIT, unknown: true)
                  }
                ";
                _.Error(Rule.UnknownArgMessage("whoknows", "doesKnowCommand", "Dog", null), 3, 37);
                _.Error(Rule.UnknownArgMessage("unknown", "doesKnowCommand", "Dog", null), 3, 67);
            });
        }

        [Fact]
        public void unknown_args_deeply()
        {
            ShouldFailRule(_ =>
            {
                _.Query = @"
                  {
                    dog {
                      doesKnowCommand(unknown: true)
                    }
                    human {
                      pet {
                        ... on Dog {
                          doesKnowCommand(unknown: true)
                        }
                      }
                    }
                  }
                ";
                _.Error(Rule.UnknownArgMessage("unknown", "doesKnowCommand", "Dog", null), 4, 39);
                _.Error(Rule.UnknownArgMessage("unknown", "doesKnowCommand", "Dog", null), 9, 43);
            });
        }
    }
}
