using System;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Errors
{
    public class NullBubblingTests : QueryTestBase<NullBubblingTests.TestSchema>
    {
        [Fact]
        public void should_bubble_null_to_nearest_nullable()
        {
            var result = AssertQueryWithErrors(@"{ testSubWithNonNullableProperty { one two } }",
                "{ 'testSubWithNonNullableProperty': null }", expectedErrorCount: 1);
            result.Errors.Count.ShouldBe(1);
        }

        public class TestSchema : Schema
        {
            public TestSchema()
            {
                Query = new TestQuery();
            }
        }

        public class TestQuery : ObjectGraphType
        {
            public TestQuery()
            {
                Name = "Query";

                Field<TestSubObjectWithNonNullableProperty>()
                    .Name("testSubWithNonNullableProperty")
                    .Resolve(_ => new { One = "One", Two = "Two" });
            }
        }

        public class TestSubObjectWithNonNullableProperty : ObjectGraphType
        {
            public TestSubObjectWithNonNullableProperty()
            {
                Name = "Sub";
                Field<StringGraphType>()
                    .Name("one");

                Field<NonNullGraphType<StringGraphType>>()
                    .Name("two")
                    .Resolve(_ => throw new Exception("wat"));
            }
        }
    }
}
