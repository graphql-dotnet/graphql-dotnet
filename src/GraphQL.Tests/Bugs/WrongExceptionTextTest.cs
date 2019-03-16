using GraphQL.Types;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public sealed class WrongExceptionTextTest : QueryTestBase<WrongExceptionTextTest.WrongExceptionTestSchema>
    {
        public sealed class WrongExceptionTestSchema : Schema
        {
            public WrongExceptionTestSchema()
            {
                var query = new ObjectGraphType();
                query.Field<TypeAGraphType>("typeA", resolve: _ => new TypeB()); // Here's a misuse
                Query = query;
            }
        }

        private sealed class TypeA { }

        private sealed class TypeB { }

        private sealed class TypeAGraphType : ObjectGraphType<TypeA>
        {
            public TypeAGraphType() => Field<StringGraphType>("test", resolve: _ => null);
        }

        [Fact]
        public void wrong_return_type_exception_text_on_complexType()
        {
            var expectedResult = new ExecutionErrors {
                new ExecutionError($"Expected value of type \"{typeof(TypeA).FullName}\" for \"{typeof(TypeAGraphType).Name}\" but got: \"{typeof(TypeB).FullName}\".")
            };

            AssertQueryErrors("{ typeA { test } }", expectedResult);
        }
    }
}
