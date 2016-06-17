using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation
{
    public class UniqueOperationNamesTests : ValidationTestBase<ValidationSchema>
    {
        private readonly UniqueOperationNames _rule = new UniqueOperationNames();

        [Test]
        public void no_operations()
        {
            var query = @"
                fragment fragA on Type {
                  field
                }
                ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void one_anon_operation()
        {
            var query = @"
                {
                  field
                }
                ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void one_named_operation()
        {
            var query = @"
                query Foo {
                  field
                }
                ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void multiple_operations()
        {
            var query = @"
                query Foo {
                  field
                }

                query Bar {
                  field
                }
                ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void multiple_operations_of_different_types()
        {
            var query = @"
                query Foo {
                  field
                }

                mutation Bar {
                  field
                }

                subscription Baz {
                  field
                }
                ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void fragment_and_operation_named_the_same()
        {
            var query = @"
                query Foo {
                  ...Foo
                }

                fragment Foo on Type {
                  field
                }
                ";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void multiple_operations_of_same_name()
        {
            var query = @"
                query Foo {
                  fieldA
                }

                query Foo {
                  fieldB
                }
                ";

            ShouldFailRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
                _.Error(_rule.DuplicateOperationNameMessage("Foo"), 6, 17);
            });
        }

        [Test]
        public void multiple_operations_of_same_name_of_diferent_types_mutation()
        {
            var query = @"
                query Foo {
                  fieldA
                }

                mutation Foo {
                  fieldB
                }
                ";

            ShouldFailRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
                _.Error(_rule.DuplicateOperationNameMessage("Foo"), 6, 17);
            });
        }

        [Test]
        public void multiple_operations_of_same_name_of_diferent_types_subscription()
        {
            var query = @"
                query Foo {
                  fieldA
                }

                subscription Foo {
                  fieldB
                }
                ";

            ShouldFailRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
                _.Error(_rule.DuplicateOperationNameMessage("Foo"), 6, 17);
            });
        }
    }
}
