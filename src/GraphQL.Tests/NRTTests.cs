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
            var type = typeof(NullableTestClass);
            var field = type.GetMethod("Field2").ShouldNotBeNull();
            var returnParameter = field.ReturnParameter;
            var context = new NullabilityInfoContext();
            var info = context.Create(returnParameter);

            //test 1
            info.Type.ShouldBe(typeof(Tuple<Tuple<string, string>, string>));
            info.ReadState.ShouldBe(NullabilityState.NotNull);
            info.GenericTypeArguments.Length.ShouldBe(2);

            //test 2
            info.GenericTypeArguments[0].Type.ShouldBe(typeof(Tuple<string, string>));
            info.GenericTypeArguments[0].ReadState.ShouldBe(NullabilityState.NotNull);
            info.GenericTypeArguments[0].GenericTypeArguments.Length.ShouldBe(2);

            //test 3
            info.GenericTypeArguments[0].GenericTypeArguments[0].Type.ShouldBe(typeof(string));
            info.GenericTypeArguments[0].GenericTypeArguments[0].ReadState.ShouldBe(NullabilityState.Nullable);

            //test 4
            info.GenericTypeArguments[0].GenericTypeArguments[1].Type.ShouldBe(typeof(string));
            info.GenericTypeArguments[0].GenericTypeArguments[1].ReadState.ShouldBe(NullabilityState.Nullable);

            //test 5
            info.GenericTypeArguments[1].Type.ShouldBe(typeof(string));
            info.GenericTypeArguments[1].ReadState.ShouldBe(NullabilityState.NotNull); //incorrectly reports nullable
        }

        public class NullableTestClass
        {
            public static Tuple<Tuple<string?, string?>, string> Field2() => null!;
            /*             1      2      3        4         5
             *
             * 1: Tuple<Tuple<string, string>, string>
             *    non-null
             *
             * 2: Tuple<string, string>
             *    non-null
             *
             * 3: string
             *    nullable
             *
             * 4: string
             *    nullable
             *
             * 5: string
             *    non-null
             */
        }
    }
}
#endif
