#nullable enable
#if NET6_0_OR_GREATER
using System;
using System.Reflection;
using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    public class NRTTests
    {
        [Fact]
        public void TestNRTField2()
        {
            var type = typeof(NullableClass18);
            var field = type.GetMethod("Field2").ShouldNotBeNull();
            var returnParameter = field.ReturnParameter;
            var context = new NullabilityInfoContext();
            var info = context.Create(returnParameter);

            info.Type.ShouldBe(typeof(Tuple<Tuple<string, string>, string>));
            info.ReadState.ShouldBe(NullabilityState.NotNull);
            info.GenericTypeArguments.Length.ShouldBe(2);

            info.GenericTypeArguments[0].Type.ShouldBe(typeof(Tuple<string, string>));
            info.GenericTypeArguments[0].ReadState.ShouldBe(NullabilityState.NotNull);
            info.GenericTypeArguments[0].GenericTypeArguments.Length.ShouldBe(2);

            info.GenericTypeArguments[0].GenericTypeArguments[0].Type.ShouldBe(typeof(string));
            info.GenericTypeArguments[0].GenericTypeArguments[0].ReadState.ShouldBe(NullabilityState.Nullable);

            info.GenericTypeArguments[0].GenericTypeArguments[1].Type.ShouldBe(typeof(string));
            info.GenericTypeArguments[0].GenericTypeArguments[1].ReadState.ShouldBe(NullabilityState.Nullable);

            info.GenericTypeArguments[1].Type.ShouldBe(typeof(string));
            info.GenericTypeArguments[1].ReadState.ShouldBe(NullabilityState.NotNull); //incorrectly reports nullable
        }

        public class NullableClass18
        {
            //check ordering of nested types
            public static Tuple<Tuple<string?, string?>, string> Field2() => null!;
        }
    }
}
#endif
