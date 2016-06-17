using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation
{
    public class LoneAnonymousOperationTests : ValidationTestBase<ValidationSchema>
    {
        private readonly LoneAnonymousOperation _rule = new LoneAnonymousOperation();

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
        public void one_anon_with_fragment()
        {
            var query = @"
                {
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
        public void multiple_anon_operations()
        {
            var query = @"
                {
                  fieldA
                }

                {
                  fieldB
                }
                ";

            ShouldFailRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
                _.Error(_rule.AnonOperationNotAloneMessage(), 2, 17);
                _.Error(_rule.AnonOperationNotAloneMessage(), 6, 17);
            });
        }

        [Test]
        public void anon_operation_with_mutation()
        {
            var query = @"
                {
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
                _.Error(_rule.AnonOperationNotAloneMessage(), 2, 17);
            });
        }

        [Test]
        public void anon_operation_with_subscription()
        {
            var query = @"
                {
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
                _.Error(_rule.AnonOperationNotAloneMessage(), 2, 17);
            });
        }
    }
}
