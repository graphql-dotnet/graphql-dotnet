using GraphQL.Validation.Rules;
using Xunit;

namespace GraphQL.Tests.Validation
{
    public class UniqueDirectivesPerLocationTests : ValidationTestBase<UniqueDirectivesPerLocation, ValidationSchema>
    {
        [Fact]
        public void no_directives()
        {
            ShouldPassRule(@"
                fragment Test on Type {
                    field
                }
            ");
        }

        [Fact]
        public void unique_directives_in_different_locations()
        {
            ShouldPassRule(@"
                fragment Test on Type @directiveA {
                    field @directiveB
                }
            ");
        }

        [Fact]
        public void unique_directives_in_same_locations()
        {
            ShouldPassRule(@"
                fragment Test on Type @directiveA @directiveB {
                    field @directiveA @directiveB
                }
            ");
        }

        [Fact]
        public void same_directives_in_different_locations()
        {
            ShouldPassRule(@"
                fragment Test on Type @directiveA {
                    field @directiveA
                }
            ");
        }

        [Fact]
        public void same_directives_in_similar_locations()
        {
            ShouldPassRule(@"
                fragment Test on Type {
                    field @directiveA
                    field @directiveA
                }
            ");
        }

        [Fact]
        public void duplicare_directives_in_one_location()
        {
            ShouldFailRule(_ =>
            {
                _.Query = @"
                fragment Test on Type {
                    field @directive @directive
                }
                ";
                duplicateDirective(_, "directive", 3, 27, 3, 38);
            });
        }

        [Fact]
        public void many_duplicare_directives_in_one_location()
        {
            ShouldFailRule(_ =>
            {
                _.Query = @"
                fragment Test on Type {
                    field @directive @directive @directive
                }
                ";
                duplicateDirective(_, "directive", 3, 27, 3, 38);
                duplicateDirective(_, "directive", 3, 27, 3, 49);
            });
        }

        [Fact]
        public void different_duplicare_directives_in_one_location()
        {
            ShouldFailRule(_ =>
            {
                _.Query = @"
                fragment Test on Type {
                    field @directiveA @directiveB @directiveA @directiveB
                }
                ";
                duplicateDirective(_, "directiveA", 3, 27, 3, 51);
                duplicateDirective(_, "directiveB", 3, 39, 3, 63);
            });
        }

        [Fact]
        public void duplicare_directives_in_many_locations()
        {
            ShouldFailRule(_ =>
            {
                _.Query = @"
                fragment Test on Type @directive @directive {
                    field @directive @directive
                }
                ";
                duplicateDirective(_, "directive", 2, 39, 2, 50);
                duplicateDirective(_, "directive", 3, 27, 3, 38);
            });
        }

        private void duplicateDirective(
            ValidationTestConfig _,
            string directiveName,
            int line1,
            int column1,
            int line2,
            int column2)
        {
            _.Error(err =>
            {
                err.Message = Rule.DuplicateDirectiveMessage(directiveName);
                err.Loc(line1, column1);
                err.Loc(line2, column2);
            });
        }
    }
}
