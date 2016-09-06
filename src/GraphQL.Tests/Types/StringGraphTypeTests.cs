﻿using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Types
{
    public class StringGraphTypeTests
    {
        private readonly StringGraphType _type;

        public StringGraphTypeTests()
        {
            _type = new StringGraphType();
        }

        [Fact]
        public void trims_quotes()
        {
            _type.ParseValue("\"one two\"").ShouldEqual("one two");
        }

        [Fact]
        public void keeps_quotes_in_string()
        {
            _type.ParseValue("one \" two").ShouldEqual("one \" two");
        }

        [Fact]
        public void keeps_single_quote()
        {
            _type.ParseValue("\"").ShouldEqual("\"");
        }
    }
}
