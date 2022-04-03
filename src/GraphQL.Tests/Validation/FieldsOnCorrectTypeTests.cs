using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class FieldsOnCorrectTypeTests : ValidationTestBase<FieldsOnCorrectType, ValidationSchema>
{
    [Fact]
    public void object_field_selection()
    {
        ShouldPassRule(@"
              fragment objectFieldSelection on Dog {
                __typename
                name
              }
            ");
    }

    [Fact]
    public void aliased_object_field_selection()
    {
        ShouldPassRule(@"
              fragment aliasedObjectFieldSelection on Dog {
                tn : __typename
                otherName : name
              }
            ");
    }

    [Fact]
    public void interface_field_selection()
    {
        ShouldPassRule(@"
              fragment interfaceFieldSelection on Pet {
                __typename
                name
              }
            ");
    }

    [Fact]
    public void aliased_interface_field_selection()
    {
        ShouldPassRule(@"
              fragment interfaceFieldSelection on Pet {
                otherName : name
              }
            ");
    }

    [Fact]
    public void lying_alias_selection()
    {
        ShouldPassRule(@"
              fragment lyingAliasSelection on Dog {
                name : nickname
              }
            ");
    }

    [Fact]
    public void ignores_fields_on_unknown_type()
    {
        ShouldPassRule(@"
              fragment unknownSelection on UnknownType {
                unknownField
              }
            ");
    }

    [Fact]
    public void reports_errors_when_type_is_known_again()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment typeKnownAgain on Pet {
                    unknown_pet_field {
                      ... on Cat {
                        unknown_cat_field
                      }
                    }
                  }
                ";

            undefinedField(_, "unknown_pet_field", "Pet", line: 3, column: 21);
            undefinedField(_, "unknown_cat_field", "Cat", line: 5, column: 25);
        });
    }

    [Fact]
    public void field_not_defined_on_fragment()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fieldNotDefined on Dog {
                    meowVolume
                  }
                ";

            undefinedField(_, "meowVolume", "Dog", suggestedFields: new[] { "barkVolume" }, line: 3, column: 21);
        });
    }

    [Fact]
    public void ignores_deeply_unknown_field()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment deepFieldNotDefined on Dog {
                    unknown_field {
                      deeper_unknown_field
                    }
                  }
                ";

            undefinedField(_, "unknown_field", "Dog", line: 3, column: 21);
        });
    }

    [Fact]
    public void subfield_not_defined()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment subFieldNotDefined on Human {
                    pets {
                      unknown_field
                    }
                  }
                ";

            undefinedField(_, "unknown_field", "Pet", line: 4, column: 23);
        });
    }

    [Fact]
    public void field_not_defined_on_inline_fragment()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment fieldNotDefined on Pet {
                    ... on Dog {
                      meowVolume
                    }
                  }
                ";

            undefinedField(_, "meowVolume", "Dog", suggestedFields: new[] { "barkVolume" }, line: 4, column: 23);
        });
    }

    [Fact]
    public void aliased_field_target_not_defined()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment aliasedFieldTargetNotDefined on Dog {
                    volume : mooVolume
                  }
                ";

            undefinedField(_, "mooVolume", "Dog", suggestedFields: new[] { "barkVolume" }, line: 3, column: 21);
        });
    }

    [Fact]
    public void aliased_lying_field_target_not_defined()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment aliasedLyingFieldTargetNotDefined on Dog {
                    barkVolume : kawVolume
                  }
                ";

            undefinedField(_, "kawVolume", "Dog", suggestedFields: new[] { "barkVolume" }, line: 3, column: 21);
        });
    }

    [Fact]
    public void not_defined_on_interface()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment notDefinedOnInterface on Pet {
                    tailLength
                  }
                ";

            undefinedField(_, "tailLength", "Pet", line: 3, column: 21);
        });
    }

    [Fact]
    public void defined_on_implementors_but_not_on_interface()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment definedOnImplementorsButNotInterface on Pet {
                    nickname
                  }
                ";

            undefinedField(_, "nickname", "Pet", suggestedTypes: new[] { "Dog", "Cat" }, line: 3, column: 21);
        });
    }

    [Fact]
    public void meta_field_selection_on_union()
    {
        ShouldPassRule(@"
              fragment directFieldSelectionOnUnion on CatOrDog {
                __typename
              }
            ");
    }

    [Fact]
    public void direct_field_selection_on_union()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment directFieldSelectionOnUnion on CatOrDog {
                    directField
                  }
                ";

            undefinedField(_, "directField", "CatOrDog", line: 3, column: 21);
        });
    }

    [Fact]
    public void defined_on_implementors_queried_on_union()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment definedOnImplementorsQueriedOnUnion on CatOrDog {
                    name
                  }
                ";

            undefinedField(_, "name", "CatOrDog",
                suggestedTypes: new[] { "Canine", "Being", "Pet", "Cat", "Dog" },
                line: 3, column: 21);
        });
    }

    [Fact]
    public void valid_field_in_inline_fragment()
    {
        ShouldPassRule(@"
              fragment objectFieldSelection on Pet {
                ... on Dog {
                  name
                }
                ... {
                  name
                }
              }
            ");
    }

    private void undefinedField(
        ValidationTestConfig _,
        string field,
        string type,
        IEnumerable<string> suggestedTypes = null,
        IEnumerable<string> suggestedFields = null,
        int line = 0,
        int column = 0)
    {
        suggestedTypes ??= Enumerable.Empty<string>();
        suggestedFields ??= Enumerable.Empty<string>();

        _.Error(FieldsOnCorrectTypeError.UndefinedFieldMessage(field, type, suggestedTypes, suggestedFields), line, column);
    }
}
