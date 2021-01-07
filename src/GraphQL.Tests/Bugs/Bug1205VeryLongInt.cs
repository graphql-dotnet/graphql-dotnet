using System;
using System.Numerics;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Errors;
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
            var error = new ExecutionError("Error trying to resolve field 'int'.", new OverflowException());
            error.AddLocation(1, 3);
            error.Path = new object[] { "int" };
            var expected = new ExecutionResult
            {
                Errors = new ExecutionErrors { error },
                Data = new { @int = (object)null }
            };

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
                    new ValidationError(null, ArgumentsOfCorrectTypeError.NUMBER, "Argument \"in\" has invalid value 636474637870330463.\nExpected type \"Int\", found 636474637870330463.")
                    {
                        Code = "ARGUMENTS_OF_CORRECT_TYPE"
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
            var error = new ExecutionError("Error trying to resolve field 'long_return_bigint'.", new OverflowException());
            error.AddLocation(1, 3);
            error.Path = new object[] { "long_return_bigint" };
            var expected = new ExecutionResult
            {
                Errors = new ExecutionErrors { error },
                Data = new { long_return_bigint = (object)null }
            };

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
                    new ValidationError(null, ArgumentsOfCorrectTypeError.NUMBER, "Argument \"in\" has invalid value 636474637870330463636474637870330463636474637870330463.\nExpected type \"Long\", found 636474637870330463636474637870330463636474637870330463.")
                    {
                        Code = "ARGUMENTS_OF_CORRECT_TYPE"
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
                resolve: ctx => 636474637870330463);
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
                resolve: ctx => 636474637870330463);
            Field<LongGraphType>(
              "long_return_bigint",
              resolve: ctx => BigInteger.Parse("636474637870330463636474637870330463636474637870330463"));
            Field<LongGraphType>(
               "long_with_arg",
               arguments: new QueryArguments(new QueryArgument<LongGraphType> { Name = "in" }),
               resolve: ctx => ctx.GetArgument<long>("in"));

            Field<BigIntGraphType>(
                "bigint",
                resolve: ctx => BigInteger.Parse("636474637870330463636474637870330463636474637870330463"));
            Field<BigIntGraphType>(
               "bigint_with_arg",
               arguments: new QueryArguments(new QueryArgument<BigIntGraphType> { Name = "in" }),
               resolve: ctx => ctx.GetArgument<BigInteger>("in"));
        }
    }
}
