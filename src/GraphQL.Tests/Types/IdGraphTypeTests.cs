﻿using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Types
{
    public class IdGraphTypeTests
    {
        private IdGraphType type = new IdGraphType();

        [Test]
        public void coerces_null_to_null()
        {
            type.Coerce(null).ShouldEqual(null);
        }

        [Test]
        public void coerces_string_to_identifier()
        {
            type.Coerce("abcd").ShouldEqual("abcd");
        }

        [Test]
        public void coerces_quoted_string_to_identifier()
        {
            type.Coerce("\"12345\"").ShouldEqual("12345");
        }
    }
}
