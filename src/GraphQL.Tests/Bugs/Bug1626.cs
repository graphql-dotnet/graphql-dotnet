using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug1626
    {
        [Fact]
        public void GetArgument_Should_Not_Throw_AmbiguousMatchException()
        {
            var context = new ResolveFieldContext()
            {
                Arguments = new Dictionary<string, object>
                {
                    ["object"] = new Dictionary<string, object>
                    {
                        ["MyProperty"] = "graphql"
                    }
                }
            };

            var arg = context.GetArgument<MyDerivedType>("object");
            arg.MyProperty.ShouldBe("graphql");
            (arg as MyBaseType).MyProperty.ShouldBeNull();
        }

        public class MyBaseType
        {
            public string MyProperty { get; set; }
        }

        public class MyDerivedType : MyBaseType
        {
            public new string MyProperty { get; set; }
        }
    }
}
