using GraphQL.Validation.Rules;
using Xunit;

namespace GraphQL.Tests.Validation
{
    public class OverlappingFieldsCanBeMergedTest : ValidationTestBase<OverlappingFieldsCanBeMerged, ValidationSchema>
    {
        [Fact]
        public void Unique_fields_should_pass()
        {
            const string query = @"
                fragment uniqueFields on Dog {
                    name
                    nickname
                }
            ";
            ShouldPassRule(query);
        }

        [Fact]
        public void Identical_fields_should_pass()
        {
            const string query = @"
                fragment mergeIdenticalFields on Dog {
                    name
                    name
                }
            ";
            ShouldPassRule(query);
        }

        [Fact]
        public void Identical_fields_with_identical_args_should_pass()
        {
            const string query = @"
                fragment mergeIdenticalFieldsWithIdenticalArgs on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand(dogCommand: SIT)
                }
            ";
            ShouldPassRule(query);
        }

        [Fact]
        public void Identical_fields_with_identical_directives_should_pass()
        {
            const string query = @"
                fragment mergeSameFieldsWithSameDirectives on Dog {
                    name @include(if: true)
                    name @include(if: true)
                }
            ";
            ShouldPassRule(query);
        }

        [Fact]
        public void Different_args_with_different_aliases_should_pass()
        {
            const string query = @"
                fragment differentArgsWithDifferentAliases on Dog {
                    knowsSit: doesKnowCommand(dogCommand: SIT)
                    knowsDown: doesKnowCommand(dogCommand: DOWN)
                }
            ";
            ShouldPassRule(query);
        }


        [Fact]
        public void Different_directives_with_different_aliases_should_pass()
        {
            const string query = @"
                fragment differentDirectivesWithDifferentAliases on Dog {
                    nameIfTrue: name @include(if: true)
                    nameIfFalse: name @include(if: false)
                }
            ";
            ShouldPassRule(query);
        }

        [Fact]
        public void Different_skip_or_include_directives_accepted_should_pass()
        {
            const string query = @"
                fragment differentDirectivesWithDifferentAliases on Dog {
                    name @include(if: true)
                    name @include(if: false)
                }
            ";
            ShouldPassRule(query);
        }

        [Fact]
        public void Same_aliases_allowed_on_non_overlapping_fields_should_pass()
        {
            const string query = @"
                fragment sameAliasesWithDifferentFieldTargets on Pet {
                    ... on Dog {
                        name
                    }
                    ... on Cat {
                        name: nickname
                    }
                }
            ";
            ShouldPassRule(query);
        }

        [Fact]
        public void Same_aliases_with_different_field_targets_should_fail()
        {
            const string query = @"
                fragment sameAliasesWithDifferentFieldTargets on Dog {
                    fido: name
                    fido: nickname
                }
            ";
            ShouldFailRule(config =>
            {
                config.Query = query;
                config.Error(e =>
                {
                    e.Message = OverlappingFieldsCanBeMerged.FieldsConflictMessage("fido", new OverlappingFieldsCanBeMerged.ConflictReason
                    {
                        Message = new OverlappingFieldsCanBeMerged.Message
                        {
                            Msg = "name and nickname are different fields"
                        }
                    });
                    e.Locations.Add(new ErrorLocation() { Line = 3, Column = 21 });
                    e.Locations.Add(new ErrorLocation() { Line = 4, Column = 21 });
                });
            });
        }

        [Fact]
        public void Alias_masking_direct_field_access_should_fail()
        {
            const string query = @"
                fragment aliasMaskingDirectFieldAccess on Dog {
                    name: nickname
                    name
                }
            ";
            ShouldFailRule(config =>
            {
                config.Query = query;
                config.Error(e =>
                {
                    e.Message = OverlappingFieldsCanBeMerged.FieldsConflictMessage("name", new OverlappingFieldsCanBeMerged.ConflictReason
                    {
                        Message = new OverlappingFieldsCanBeMerged.Message
                        {
                            Msg = "nickname and name are different fields"
                        }
                    });
                    e.Locations.Add(new ErrorLocation() { Line = 3, Column = 21 });
                    e.Locations.Add(new ErrorLocation() { Line = 4, Column = 21 });
                });
            });
        }
    }
}
