using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation
{
    public class VariablesInAllowedPositionTests : ValidationTestBase<ValidationSchema>
    {
        private readonly VariablesInAllowedPosition _rule = new VariablesInAllowedPosition();

        [Test]
        public void boolean_to_boolean()
        {
            var query = @"
                query Query($booleanArg: Boolean)
                {
                  complicatedArgs {
                    booleanArgField(booleanArg: $booleanArg)
                  }
                }
                ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void boolean_to_boolean_within_fragment()
        {
            var query = @"
              fragment booleanArgFrag on ComplicatedArgs {
                booleanArgField(booleanArg: $booleanArg)
              }
              query Query($booleanArg: Boolean)
              {
                complicatedArgs {
                  ...booleanArgFrag
                }
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });

            var query2 = @"
              query Query($booleanArg: Boolean)
              {
                complicatedArgs {
                  ...booleanArgFrag
                }
              }
              fragment booleanArgFrag on ComplicatedArgs {
                booleanArgField(booleanArg: $booleanArg)
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query2;
                _.Rule(_rule);
            });
        }

        [Test]
        public void nonnull_boolean_to_boolean()
        {
            var query = @"
              query Query($nonNullBooleanArg: Boolean!)
              {
                complicatedArgs {
                  booleanArgField(booleanArg: $nonNullBooleanArg)
                }
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void nonnull_boolean_to_boolean_within_fragment()
        {
            var query = @"
              fragment booleanArgFrag on ComplicatedArgs {
                booleanArgField(booleanArg: $nonNullBooleanArg)
              }
              query Query($nonNullBooleanArg: Boolean!)
              {
                complicatedArgs {
                  ...booleanArgFrag
                }
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void int_to_int_with_default()
        {
            var query = @"
              query Query($intArg: Int = 1)
              {
                complicatedArgs {
                  nonNullIntArgField(nonNullIntArg: $intArg)
                }
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void string_list_to_string_list()
        {
            var query = @"
              query Query($stringListVar: [String])
              {
                complicatedArgs {
                  stringListArgField(stringListArg: $stringListVar)
                }
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void nonnull_string_list_to_string_list()
        {
            var query = @"
              query Query($stringListVar: [String!])
              {
                complicatedArgs {
                  stringListArgField(stringListArg: $stringListVar)
                }
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void string_to_string_list_in_item_position()
        {
            var query = @"
              query Query($stringVar: String)
              {
                complicatedArgs {
                  stringListArgField(stringListArg: [$stringVar])
                }
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void nonnull_string_to_string_list_in_item_position()
        {
            var query = @"
              query Query($stringVar: String!)
              {
                complicatedArgs {
                  stringListArgField(stringListArg: [$stringVar])
                }
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void complexinput_to_complexinput()
        {
            var query = @"
              query Query($complexVar: ComplexInput)
              {
                complicatedArgs {
                  complexArgField(complexArg: $complexVar)
                }
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void complexinput_to_complexinput_in_field_position()
        {
            var query = @"
              query Query($boolVar: Boolean = false)
              {
                complicatedArgs {
                  complexArgField(complexArg: {requiredArg: $boolVar})
                }
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void nullable_boolean_to_nullable_boolean_in_directive()
        {
            var query = @"
              query Query($boolVar: Boolean!)
              {
                dog @include(if: $boolVar)
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void boolean_to_nullable_boolean_in_directive_with_default()
        {
            var query = @"
              query Query($boolVar: Boolean = false)
              {
                dog @include(if: $boolVar)
              }
            ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void int_to_nonnull_int()
        {
            var query = @"
              query Query($intArg: Int) {
                complicatedArgs {
                  nonNullIntArgField(nonNullIntArg: $intArg)
                }
              }
            ";

            ShouldFailRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
                _.Error(err =>
                {
                    err.Message = _rule.BadVarPosMessage("intArg", "Int", "Int!");
                    err.Add(2, 27);
                    err.Add(4, 53);
                });
            });
        }
    }
}
