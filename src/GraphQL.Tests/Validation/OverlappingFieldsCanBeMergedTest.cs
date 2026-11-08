using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class OverlappingFieldsCanBeMergedTest : ValidationTestBase<OverlappingFieldsCanBeMerged, ValidationSchema>
{
    [Fact]
    public void Unique_fields_should_pass()
    {
        const string query = """
            fragment uniqueFields on Dog {
              name
              nickname
            }
            """;
        ShouldPassRule(query);
    }

    [Fact]
    public void Identical_fields_should_pass()
    {
        const string query = """
            fragment mergeIdenticalFields on Dog {
              name
              name
            }
            """;
        ShouldPassRule(query);
    }

    [Fact]
    public void Identical_fields_with_identical_args_should_pass()
    {
        const string query = """
            fragment mergeIdenticalFieldsWithIdenticalArgs on Dog {
              doesKnowCommand(dogCommand: SIT)
              doesKnowCommand(dogCommand: SIT)
            }
            """;
        ShouldPassRule(query);
    }

    [Fact]
    public void Identical_fields_with_identical_directives_should_pass()
    {
        const string query = """
            fragment mergeSameFieldsWithSameDirectives on Dog {
              name @include(if: true)
              name @include(if: true)
            }
            """;
        ShouldPassRule(query);
    }

    [Fact]
    public void Different_args_with_different_aliases_should_pass()
    {
        const string query = """
            fragment differentArgsWithDifferentAliases on Dog {
              knowsSit: doesKnowCommand(dogCommand: SIT)
              knowsDown: doesKnowCommand(dogCommand: DOWN)
            }
            """;
        ShouldPassRule(query);
    }

    [Fact]
    public void Different_directives_with_different_aliases_should_pass()
    {
        const string query = """
            fragment differentDirectivesWithDifferentAliases on Dog {
              nameIfTrue: name @include(if: true)
              nameIfFalse: name @include(if: false)
            }
            """;
        ShouldPassRule(query);
    }

    [Fact]
    public void Different_skip_or_include_directives_accepted_should_pass()
    {
        const string query = """
            fragment differentDirectivesWithDifferentAliases on Dog {
              name @include(if: true)
              name @include(if: false)
            }
            """;
        ShouldPassRule(query);
    }

    [Fact]
    public void Same_aliases_allowed_on_non_overlapping_fields_should_pass()
    {
        const string query = """
            fragment sameAliasesWithDifferentFieldTargets on Pet {
                ... on Dog {
                    name
                }
                ... on Cat {
                    name: nickname
                }
            }
            """;
        ShouldPassRule(query);
    }

    [Fact]
    public void Same_aliases_with_different_field_targets_should_fail()
    {
        const string query = """
            fragment sameAliasesWithDifferentFieldTargets on Dog {
                fido: name
                fido: nickname
            }
            """;
        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("fido", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "name and nickname are different fields"
                    }
                });
                e.Locations.Add(new Location(2, 5));
                e.Locations.Add(new Location(3, 5));
            });
        });
    }

    [Fact]
    public void Alias_masking_direct_field_access_should_fail()
    {
        const string query = """
            fragment aliasMaskingDirectFieldAccess on Dog {
                name: nickname
                name
            }
            """;
        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("name", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "nickname and name are different fields"
                    }
                });
                e.Locations.Add(new Location(2, 5));
                e.Locations.Add(new Location(3, 5));
            });
        });
    }

    [Fact]
    public void Different_args_second_adds_an_argument_should_fail()
    {
        const string query = """
            fragment conflictingArgs on Dog {
                doesKnowCommand
                doesKnowCommand(dogCommand: HEEL)
            }
            """;

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("doesKnowCommand", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "they have differing arguments"
                    }
                });
                e.Locations.Add(new Location(2, 5));
                e.Locations.Add(new Location(3, 5));
            });
        });
    }

    [Fact]
    public void Different_args_second_missing_an_argument_should_fail()
    {
        const string query = """
            fragment conflictingArgs on Dog {
                doesKnowCommand(dogCommand: SIT)
                doesKnowCommand
            }
            """;

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("doesKnowCommand", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "they have differing arguments"
                    }
                });
                e.Locations.Add(new Location(2, 5));
                e.Locations.Add(new Location(3, 5));
            });
        });
    }

    [Fact]
    public void Conflicting_args_should_fail()
    {
        const string query = """
            fragment conflictingArgs on Dog {
                doesKnowCommand(dogCommand: SIT)
                doesKnowCommand(dogCommand: HEEL)
            }
            """;

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("doesKnowCommand", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "they have differing arguments"
                    }
                });
                e.Locations.Add(new Location(2, 5));
                e.Locations.Add(new Location(3, 5));
            });
        });
    }

    /// <summary>
    /// This is valid since no object can be both a "Dog" and a "Cat", thus
    /// these fields can never overlap.
    /// </summary>
    [Fact]
    public void Allows_different_args_where_no_conflict_is_possible_should_pass()
    {
        const string query = """
            fragment conflictingArgs on Pet {
                ... on Dog {
                    name(surname: "test")
                }
                ... on Cat {
                    name
                }
            }
            """;

        ShouldPassRule(query);
    }

    [Fact]
    public void Encounters_conflict_in_fragments_should_fail()
    {
        const string query = """
            {
                ...A
                ...B
            }
            fragment A on Type {
                x: a
            }
            fragment B on Type {
                x: b
            }
            """;

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("x", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "a and b are different fields"
                    }
                });
                e.Locations.Add(new Location(6, 5));
                e.Locations.Add(new Location(9, 5));
            });
        });
    }

    [Fact]
    public void Reports_conflicts_as_they_are_encountered_in_separate_selection_sets()
    {
        const string query = """
            {
                f1 {
                    ...A
                    ...B
                }
                f2 {
                    ...B
                    ...A
                }
                f3 {
                    ...A
                    ...B
                    x: c
                }
            }
            fragment A on Type {
                x: a
            }
            fragment B on Type {
                x: b
            }
            """;

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("x", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "a and b are different fields"
                    }
                });
                e.Locations.Add(new Location(17, 5));
                e.Locations.Add(new Location(20, 5));
            });
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("x", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "b and a are different fields"
                    }
                });
                e.Locations.Add(new Location(20, 5));
                e.Locations.Add(new Location(17, 5));
            });
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("x", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "a and b are different fields"
                    }
                });
                e.Locations.Add(new Location(17, 5));
                e.Locations.Add(new Location(20, 5));
            });
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("x", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "a and c are different fields"
                    }
                });
                e.Locations.Add(new Location(17, 5));
                e.Locations.Add(new Location(13, 9));
            });
        });
    }

    [Fact]
    public void Deep_conflict()
    {
        const string query = """
            {
                field {
                    x: a
                },
                field {
                    x: b
                }
            }
            """;

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("x", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "a and b are different fields"
                    }
                });
                e.Locations.Add(new Location(3, 9));
                e.Locations.Add(new Location(6, 9));
            });
        });
    }

    [Fact]
    public void Deep_conflict_with_multiple_issues_should_fail()
    {
        const string query = """
            {
                field {
                    x: a
                    y: c
                },
                field {
                    x: b
                    y: d
                }
            }
            """;

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("x", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "a and b are different fields"
                    }
                });
                e.Locations.Add(new Location(3, 9));
                e.Locations.Add(new Location(7, 9));
            });
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("y", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "c and d are different fields"
                    }
                });
                e.Locations.Add(new Location(4, 9));
                e.Locations.Add(new Location(8, 9));
            });
        });
    }

    [Fact]
    public void Very_deep_conflict_should_fail()
    {
        const string query = """
            {
                field {
                    deepField {
                        x: a
                    }
                },
                field {
                    deepField {
                        x: b
                    }
                }
            }
            """;

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("x", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "a and b are different fields"
                    }
                });
                e.Locations.Add(new Location(4, 13));
                e.Locations.Add(new Location(9, 13));
            });
        });
    }

    [Fact]
    public void Reports_deep_conflict_at_the_conflicting_fields()
    {
        const string query = """
            {
                field {
                    deepField {
                        x: a
                    }
                    deepField {
                        x: b
                    }
                },
                field {
                    deepField {
                        y
                    }
                }
            }
            """;

        ShouldReportConflictWitnesses(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("x", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "a and b are different fields"
                    }
                });
                e.Locations.Add(new Location(4, 13));
                e.Locations.Add(new Location(7, 13));
            });
        });
    }

    [Fact]
    public void Reports_deep_fragment_conflict_at_the_conflicting_fields()
    {
        const string query = """
            {
                field {
                    ...F
                }
                field {
                    ...F
                }
            }
            fragment F on T {
                deepField {
                    deeperField {
                        x: a
                    }
                    deeperField {
                        x: b
                    }
                },
                deepField {
                    deeperField {
                        y
                    }
                }
            }
            """;

        ShouldReportConflictWitnesses(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("x", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "a and b are different fields"
                    }
                });
                e.Locations.Add(new Location(12, 13));
                e.Locations.Add(new Location(15, 13));
            });
        });
    }

    [Fact]
    public void Reports_deep_conflict_in_nested_fragments()
    {
        const string query = """
            {
                field {
                    ...F
                }
                field {
                    ...I
                }
            }
            fragment F on T {
                x: a
                ...G
            }
            fragment G on T {
                y: c
            }
            fragment I on T {
                y: d
                ...J
            }
            fragment J on T {
                x: b
            }
            """;

        ShouldFailRule(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("y", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "c and d are different fields"
                    }
                });
                e.Locations.Add(new Location(14, 5));
                e.Locations.Add(new Location(17, 5));
            });
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("x", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "a and b are different fields"
                    }
                });
                e.Locations.Add(new Location(10, 5));
                e.Locations.Add(new Location(21, 5));
            });
        });
    }

    [Fact]
    public void Ignores_unknown_fragments()
    {
        const string query = """
            {
                field
                ...Unknown
                ...Known
            }
            fragment Known on T {
                field
                ...OtherUnknown
            }
            """;

        ShouldPassRule(query);
    }

    [Fact]
    public void Does_not_infinite_loop_on_recursive_fragment()
    {
        const string query = """
            fragment fragA on Human {
                name,
                relatives {
                    name,
                    ...fragA
                }
            }
            """;

        ShouldPassRule(query);
    }

    [Fact]
    public void Does_not_infinite_loop_on_immediately_recursive_fragment()
    {
        const string query = """
            fragment fragA on Human {
                name,
                ...fragA
            }
            """;

        ShouldPassRule(query);
    }

    [Fact]
    public void Does_not_infinite_loop_on_transitively_recursive_fragment()
    {
        const string query = """
            fragment fragA on Human { name, ...fragB }
            fragment fragB on Human { name, ...fragC }
            fragment fragC on Human { name, ...fragA }
            """;

        ShouldPassRule(query);
    }

    [Fact]
    public void Finds_invalid_case_even_with_immediately_recursive_fragment()
    {
        const string query = """
            fragment sameAliasesWithDifferentFieldTargets on Dog {
                ...sameAliasesWithDifferentFieldTargets
                fido: name
                fido: nickname
            }
            """;

        ShouldReportConflictWitnesses(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("fido", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "name and nickname are different fields"
                    }
                });
                e.Locations.Add(new Location(3, 5));
                e.Locations.Add(new Location(4, 5));
            });
        });
    }

    [Fact]
    public void Conflicting_return_types_which_potentially_overlap()
    {
        ISchema schema = new ResultTypeValidationSchema();

        const string query = """
            {
                someBox {
                    ...on IntBox {
                        scalar
                    }
                    ...on NonNullStringBox1 {
                        scalar
                    }
                }
            }
            """;

        ShouldFailRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("scalar", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "they return conflicting types Int and String!"
                    }
                });
                e.Locations.Add(new Location(4, 13));
                e.Locations.Add(new Location(7, 13));
            });
        });
    }

    [Fact]
    public void Compatible_return_shapes_on_different_return_types()
    {
        ISchema schema = new ResultTypeValidationSchema();

        const string query = """
            {
                someBox {
                    ... on SomeBox {
                        deepBox {
                            unrelatedField
                        }
                    }
                    ... on StringBox {
                        deepBox {
                            unrelatedField
                        }
                    }
                }
            }
            """;

        ShouldPassRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
        });
    }

    [Fact]
    public void Disallows_differing_return_types_despite_no_overlap()
    {
        ISchema schema = new ResultTypeValidationSchema();

        const string query = """
            {
                someBox {
                    ... on IntBox {
                        scalar
                    }
                    ... on StringBox {
                        scalar
                    }
                }
            }
            """;

        ShouldFailRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("scalar", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "they return conflicting types Int and String"
                    }
                });
                e.Locations.Add(new Location(4, 13));
                e.Locations.Add(new Location(7, 13));
            });
        });
    }

    [Fact]
    public void Reports_correctly_when_a_non_exclusive_follows_an_exclusive()
    {
        ISchema schema = new ResultTypeValidationSchema();

        const string query = """
            {
                someBox {
                    ... on IntBox {
                        deepBox {
                            ...X
                        }
                    }
                }
                someBox {
                    ... on StringBox {
                        deepBox {
                            ...Y
                        }
                    }
                }
                memoed: someBox {
                    ... on IntBox {
                        deepBox {
                            ...X
                        }
                    }
                }
                memoed: someBox {
                    ... on StringBox {
                        deepBox {
                            ...Y
                        }
                    }
                }
                other: someBox {
                    ...X
                }
                other: someBox {
                    ...Y
                }
            }
            fragment X on SomeBox {
                scalar
            }
            fragment Y on SomeBox {
                scalar: unrelatedField
            }
            """;

        ShouldFailRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("scalar", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "scalar and unrelatedField are different fields"
                    }
                });
                e.Locations.Add(new Location(38, 5));
                e.Locations.Add(new Location(41, 5));
            });
        });
    }

    [Fact]
    public void Disallows_differing_return_type_nullability_despite_no_overlap()
    {
        ISchema schema = new ResultTypeValidationSchema();

        const string query = """
            {
                someBox {
                    ... on NonNullStringBox1 {
                        scalar
                    }
                    ... on StringBox {
                        scalar
                    }
                }
            }
            """;

        ShouldFailRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("scalar", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "they return conflicting types String! and String"
                    }
                });
                e.Locations.Add(new Location(4, 13));
                e.Locations.Add(new Location(7, 13));
            });
        });
    }

    [Fact]
    public void Disallows_differing_return_type_list_despite_no_overlap()
    {
        ISchema schema = new ResultTypeValidationSchema();

        string query = """
            {
                someBox {
                    ... on IntBox {
                        box: listStringBox {
                            scalar
                        }
                    }
                    ... on StringBox {
                        box: stringBox {
                            scalar
                        }
                    }
                }
            }
            """;

        ShouldFailRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("box", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "they return conflicting types [StringBox] and StringBox"
                    }
                });
                e.Locations.Add(new Location(4, 13));
                e.Locations.Add(new Location(9, 13));
            });
        });

        query = """
            {
                someBox {
                    ... on IntBox {
                        box: stringBox {
                            scalar
                        }
                    }
                    ... on StringBox {
                        box: listStringBox {
                            scalar
                        }
                    }
                }
            }
            """;

        ShouldFailRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("box", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "they return conflicting types StringBox and [StringBox]"
                    }
                });
                e.Locations.Add(new Location(4, 13));
                e.Locations.Add(new Location(9, 13));
            });
        });
    }

    [Fact]
    public void Disallows_differing_subfields()
    {
        ISchema schema = new ResultTypeValidationSchema();

        const string query = """
            {
                someBox {
                    ... on IntBox {
                        box: stringBox {
                            val: scalar
                            val: unrelatedField
                        }
                    }
                    ... on StringBox {
                        box: stringBox {
                            val: scalar
                        }
                    }
                }
            }
            """;

        ShouldFailRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("val", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "scalar and unrelatedField are different fields"
                    }
                });
                e.Locations.Add(new Location(5, 17));
                e.Locations.Add(new Location(6, 17));
            });
        });
    }

    [Fact]
    public void Disallows_differing_deep_return_types_despite_no_overlap()
    {
        ISchema schema = new ResultTypeValidationSchema();

        const string query = """
            {
                someBox {
                    ... on IntBox {
                        box: stringBox {
                            scalar
                        }
                    }
                    ... on StringBox {
                        box: intBox {
                            scalar
                        }
                    }
                }
            }
            """;

        ShouldFailRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("scalar", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "they return conflicting types String and Int"
                    }
                });
                e.Locations.Add(new Location(5, 17));
                e.Locations.Add(new Location(10, 17));
            });
        });
    }

    [Fact]
    public void Allows_non_conflicting_overlapping_types()
    {
        ISchema schema = new ResultTypeValidationSchema();

        const string query = """
            {
                someBox {
                    ... on IntBox {
                        scalar: unrelatedField
                    }
                    ... on StringBox {
                        scalar
                    }
                }
            }
            """;

        ShouldPassRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
        });
    }

    [Fact]
    public void Same_wrapped_scalar_return_types()
    {
        ISchema schema = new ResultTypeValidationSchema();

        const string query = """
            {
                someBox {
                    ...on NonNullStringBox1 {
                        scalar
                    }
                    ...on NonNullStringBox2 {
                        scalar
                    }
                }
            }
            """;

        ShouldPassRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
        });
    }

    [Fact]
    public void Allows_inline_typeless_fragments()
    {
        ISchema schema = new ResultTypeValidationSchema();

        const string query = """
            {
                a
                ... {
                    a
                }
            }
            """;

        ShouldPassRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
        });
    }

    [Fact]
    public void Ignores_unknown_types()
    {
        ISchema schema = new ResultTypeValidationSchema();

        const string query = """
            {
                someBox {
                    ...on UnknownType {
                        scalar
                    }
                    ...on NonNullStringBox2 {
                        scalar
                    }
                }
            }
            """;

        ShouldPassRule(config =>
        {
            config.Schema = schema;
            config.Query = query;
        });
    }

    /// <summary>
    /// When fragment A (defined first) spreads fragment B (defined second),
    /// and the conflict lives in B's sub-selections, the conflict must still be detected
    /// regardless of fragment ordering in the document.
    /// </summary>
    [Fact]
    public void Deep_conflict_detected_when_spreading_fragment_defined_after_parent_fragment()
    {
        // Fragment A is defined BEFORE fragment B.
        // A has two `human` selections: one spreads B, one has a direct sub-selection.
        // The conflict is at the `relatives.name` level: B returns `name` (String),
        // A's direct branch returns `name: dummy` (String), a different underlying field.
        const string query = """
            query { ...A }
            fragment A on Query {
                human { ...B }
                human { relatives { name: dummy } }
            }
            fragment B on Human {
                relatives { name }
            }
            """;

        ShouldReportConflictWitnesses(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("name", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "name and dummy are different fields"
                    }
                });
                e.Locations.Add(new Location(7, 17));
                e.Locations.Add(new Location(4, 25));
            });
        });
    }

    /// <summary>
    /// Same as <see cref="Deep_conflict_detected_when_spreading_fragment_defined_after_parent_fragment"/>
    /// but with fragment B defined BEFORE fragment A, to confirm the result is order-independent.
    /// </summary>
    [Fact]
    public void Deep_conflict_detected_when_spreading_fragment_defined_before_parent_fragment()
    {
        // Fragment B is defined BEFORE fragment A.
        const string query = """
            query { ...A }
            fragment B on Human {
                relatives { name }
            }
            fragment A on Query {
                human { ...B }
                human { relatives { name: dummy } }
            }
            """;

        ShouldReportConflictWitnesses(config =>
        {
            config.Query = query;
            config.Error(e =>
            {
                e.Message = OverlappingFieldsCanBeMergedError.FieldsConflictMessage("name", new OverlappingFieldsCanBeMerged.ConflictReason
                {
                    Message = new OverlappingFieldsCanBeMerged.Message
                    {
                        Msg = "name and dummy are different fields"
                    }
                });
                e.Locations.Add(new Location(3, 17));
                e.Locations.Add(new Location(7, 25));
            });
        });
    }
    private void ShouldReportConflictWitnesses(Action<ValidationTestConfig> configure)
    {
        var config = new ValidationTestConfig();
        configure(config);
        var document = new GraphQLDocumentBuilder().Build(config.Query);
        var result = new DocumentValidator().ValidateAsync(new ValidationOptions
        {
            Schema = config.Schema ?? Schema,
            Document = document,
            Operation = document.Definitions.OfType<GraphQLOperationDefinition>().FirstOrDefault()!,
            Rules = [Rule],
            Variables = config.Variables,
        }).GetAwaiter().GetResult();

        result.IsValid.ShouldBeFalse();
        // A merged selection and its local AST can encounter the same conflicting
        // pair. Preserve the concrete diagnostics without requiring global deduplication.
        var actual = result.Errors.Select(error => error.Message + " " + string.Join(",",
            error.Locations.ShouldNotBeNull().Select(location => $"{location.Line}:{location.Column}"))).Distinct();
        var expected = config.Assertions.Select(error => error.Message + " " + string.Join(",",
            error.Locations.Select(location => $"{location.Line}:{location.Column}")));
        actual.ShouldBe(expected);
    }

}
