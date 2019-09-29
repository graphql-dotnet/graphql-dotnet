using GraphQL.Types;
using Shouldly;
using System;
using System.Numerics;
using Xunit;

namespace GraphQL.Tests.Bugs
{
    // https://github.com/graphql-dotnet/graphql-dotnet/issues/1205
    // https://github.com/graphql-dotnet/graphql-dotnet/issues/1022
    public class Bug1205VeryLongInt : QueryTestBase<Bug1205VeryLongIntSchema>
    {
        [Fact]
        public void Very_Long_Number_Should_Return_Error_For_Int()
        {
            var query = "{ int }";
            var expected = new ExecutionResult { Errors = new ExecutionErrors { new ExecutionError("Value was either too large or too small for an Int32.", new OverflowException()) } };
            AssertQueryIgnoreErrors(query, expected, renderErrors: true, expectedErrorCount: 1);
        }

        [Fact]
        public void Very_Long_Number_In_Input_Should_Return_Error_For_Int()
        {
            var query = "{ int_with_arg(in:636474637870330463) }";
            var expected = new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError("Argument \"in\" has invalid value 636474637870330463.\nExpected type \"Int\", found 636474637870330463.")
                    {
                        Code = "5.3.3.1"
                    }
                }
            };
            expected.Errors[0].AddLocation(1, 16);

            AssertQueryIgnoreErrors(query, expected, renderErrors: true, expectedErrorCount: 1);
        }

        [Fact]
        public void Very_Long_Number_Should_Return_As_Is_For_Long()
        {
            var query = "{ long }";
            var expected = @"{
  ""long"": 636474637870330463 
}";
            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void Very_Long_Number_In_Input_Should_Work_For_Long()
        {
            var query = "{ long_with_arg(in:636474637870330463) }";
            var expected = @"{
  ""long_with_arg"": 636474637870330463 
}";
            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void Very_Very_Long_Number_Should_Return_Error_For_Long()
        {
            var query = "{ long_return_bigint }";
            var expected = new ExecutionResult { Errors = new ExecutionErrors { new ExecutionError("Value was either too large or too small for an Int64.", new OverflowException()) } };
            AssertQueryIgnoreErrors(query, expected, renderErrors: true, expectedErrorCount: 1);
        }

        [Fact]
        public void Very_Very_Long_Number_In_Input_Should_Return_Error_For_Long()
        {
            var query = "{ long_with_arg(in:636474637870330463636474637870330463636474637870330463) }";
            var expected = new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError("Argument \"in\" has invalid value 636474637870330463636474637870330463636474637870330463.\nExpected type \"Long\", found 636474637870330463636474637870330463636474637870330463.")
                    {
                        Code = "5.3.3.1"
                    }
                }
            };
            expected.Errors[0].AddLocation(1, 17);

            AssertQueryIgnoreErrors(query, expected, renderErrors: true, expectedErrorCount: 1);
        }

        [Fact]
        public void Very_Very_Long_Number_Should_Return_As_Is_For_BigInteger()
        {
            var query = "{ bigint }";
            var expected = @"{
  ""bigint"": 636474637870330463636474637870330463636474637870330463 
}";
            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void Very__Very_Long_Number_In_Input_Should_Work_For_BigInteger()
        {
            var query = "{ bigint_with_arg(in:636474637870330463636474637870330463636474637870330463) }";
            var expected = @"{
  ""bigint_with_arg"": 636474637870330463636474637870330463636474637870330463 
}";
            AssertQuerySuccess(query, expected);
        }
    }

    public class Bug1205VeryLongIntSchema : Schema
    {
        public Bug1205VeryLongIntSchema()
        {
            Query = new Bug1205VeryLongIntQuery();
        }
    }

    public class Bug1205VeryLongIntQuery : ObjectGraphType
    {
        public Bug1205VeryLongIntQuery()
        {
            Field<IntGraphType>(
                "int",
                resolve: ctx =>
                {
                    return 636474637870330463;
                });
            Field<IntGraphType>(
               "int_with_arg",
               arguments: new QueryArguments(new QueryArgument<IntGraphType> { Name = "in" }),
               resolve: ctx =>
               {
                   Assert.True(false, "Never goes here");
                   return 1;
               });

            Field<LongGraphType>(
                "long",
                resolve: ctx =>
                {
                    return 636474637870330463;
                });
            Field<LongGraphType>(
              "long_return_bigint",
              resolve: ctx =>
              {
                  return BigInteger.Parse("636474637870330463636474637870330463636474637870330463");
              });
            Field<LongGraphType>(
               "long_with_arg",
               arguments: new QueryArguments(new QueryArgument<LongGraphType> { Name = "in" }),
               resolve: ctx =>
               {
                   return ctx.GetArgument<long>("in");
               });

            Field<BigIntegerGraphType>(
                "bigint",
                resolve: ctx =>
                {
                    return BigInteger.Parse("636474637870330463636474637870330463636474637870330463");
                });
            Field<BigIntegerGraphType>(
               "bigint_with_arg",
               arguments: new QueryArguments(new QueryArgument<BigIntegerGraphType> { Name = "in" }),
               resolve: ctx =>
               {
                   return ctx.GetArgument<BigInteger>("in");
               });
        }
    }
}
