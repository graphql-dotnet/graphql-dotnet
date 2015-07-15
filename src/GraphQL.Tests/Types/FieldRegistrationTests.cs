using System;
using GraphQL.Types;

namespace GraphQL.Tests.Types
{
    public class FieldRegistrationTests
    {
        [Test]
        public void throws_error_when_trying_to_register_field_with_same_name()
        {
            var graphType = new ObjectGraphType();
            graphType.Field<StringGraphType>("id");

            Expect.Throws<ArgumentOutOfRangeException>(
                () => graphType.Field<StringGraphType>("id")
            );
        }
    }
}
