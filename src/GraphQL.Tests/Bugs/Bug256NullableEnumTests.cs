using System.Collections.Generic;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    public class Bug256NullableEnumTests
    {
        public enum EnumType {
            A,
            B
        }

        [Fact]
        public void nullable_enum_returns_value()
        {
            var ctx = new ResolveFieldContext();
            ctx.Arguments = new Dictionary<string, object> { { "value", EnumType.B } };

            var result = ctx.GetArgument<EnumType?>("value");
            result.ShouldBe(EnumType.B);
        }

        [Fact]
        public void nullable_enum_returns_null()
        {
            var ctx = new ResolveFieldContext();
            ctx.Arguments = new Dictionary<string, object> { { "value", null } };

            var result = ctx.GetArgument<EnumType?>("value");
            result.ShouldBeNull();
        }

        [Fact]
        public void null_enum_returns_default()
        {
            var ctx = new ResolveFieldContext();
            ctx.Arguments = new Dictionary<string, object> { { "value", null } };

            var result = ctx.GetArgument<EnumType>("value");
            result.ShouldBe(EnumType.A);
        }

        [Fact]
        public void enum_returns_value()
        {
            var ctx = new ResolveFieldContext();
            ctx.Arguments = new Dictionary<string, object> { { "value", EnumType.B } };

            var result = ctx.GetArgument<EnumType>("value");
            result.ShouldBe(EnumType.B);
        }
    }
}
