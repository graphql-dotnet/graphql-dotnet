using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Tests.Types;

public class CustomScalarTests : QueryTestBase<CustomScalarSchema>
{
    [Theory]
    [InlineData("externalNull", null)] // parsing "externalNull" should return null
    [InlineData(null, "internalNull")] // parsing null should return "internalNull"
    [InlineData("hello", "hello")]     // parsing "hello" should return "hello"
    public void ParseLiteral(string externalValue, string internalValue)
    {
        var c = new CustomScalar();
        // try parsing literal AST
        c.ParseLiteral(externalValue == null ? new GraphQLNullValue() : new GraphQLStringValue(externalValue)).ShouldBe(internalValue);
    }

    [Theory]
    [InlineData("externalNull", null)] // parsing "externalNull" should return null
    [InlineData(null, "internalNull")] // parsing null should return "internalNull"
    [InlineData("hello", "hello")]     // parsing "hello" should return "hello"
    public void ParseValue(string externalValue, string internalValue)
    {
        var c = new CustomScalar();
        // try parsing value
        c.ParseValue(externalValue).ShouldBe(internalValue);
    }

    [Theory]
    [InlineData(null, "externalNull")] // serializing null should return "externalNull" (inverse of parsing)
    [InlineData("internalNull", null)] // serializing "internalNull" should return null (inverse of parsing)
    [InlineData("hello", "hello")]     // serializing "hello" should return "hello"
    public void Serialize(string internalValue, string externalValue)
    {
        var c = new CustomScalar();
        // try serializing
        c.Serialize(internalValue).ShouldBe(externalValue);
    }

    [Theory]
    [InlineData(null, "externalNull")] // serializing null should return "externalNull" (inverse of parsing)
    [InlineData("internalNull", null)] // serializing "internalNull" should return null (inverse of parsing)
    [InlineData("hello", "hello")]     // serializing "hello" should return "hello"
    public void ToAST(string internalValue, string externalValue)
    {
        var c = new CustomScalar();
        // try converting internal value to AST
        var ast = c.ToAST(internalValue);
        ast.ShouldNotBeNull();
        if (ast is GraphQLNullValue) // GraphQLNullValue.Value is 'null' ROM
            externalValue.ShouldBeNull();
        else
            ast.ShouldBeAssignableTo<IHasValueNode>().Value.ShouldBe(externalValue);
    }

    [Theory]
    [InlineData("input", "hello", "hello", Response.Success)]                     // custom scalar receives "hello" and string scalar returns "hello"
    [InlineData("input", null, "internalNull", Response.Success)]                 // custom scalar coerces null to "internalNull" and string scalar returns "internalNull"
    [InlineData("input", "externalNull", null, Response.Success)]                 // custom scalar coerces "externalNull" to null and string scalar returns null
    [InlineData("input", "error", null, Response.ErrorNoData)]                    // custom scalar throws while coercing "error", fails document validation
    [InlineData("output", "hello", "hello", Response.Success)]                    // string scalar receives "hello" and custom scalar returns "hello"
    [InlineData("output", null, "externalNull", Response.Success)]                // string scalar receives null and custom scalar returns "externalNull"
    [InlineData("output", "internalNull", null, Response.Success)]                // string scalar receives "internalNull" and custom scalar returns it as null
    [InlineData("output", "error", null, Response.Error)]                         // string scalar receives "error" and custom scalar throws attempting to convert it
    [InlineData("inputOutput", "hello", "hello", Response.Success)]               // custom scalar receives "hello" and then custom scalar returns "hello"
    [InlineData("inputOutput", null, null, Response.Success)]                     // custom scalar coerces null to "internalNull" and then custom scalar coerces "internalNull" to null
    [InlineData("inputOutput", "internalNull", null, Response.Success)]           // custom scalar receives "internalNull" and then custom scalar coerces "internalNull" to null
    [InlineData("inputOutput", "externalNull", "externalNull", Response.Success)] // custom scalar coerces "externalNull" to null and then custom scalar coerces null to "externalNull"
    [InlineData("nonNullOutput", "internalNull", null, Response.ErrorDataNull)]   // string scalar receives "internalNull" and non-null custom scalar attempts to return null (and fails)
    [InlineData("nonNullOutput", null, "externalNull", Response.Success)]         // string scalar receives null and non-null custom scalar returns "externalNull" for non-null field
    [InlineData("nonNullInput", "externalNull", null, Response.Success)]          // custom scalar coerces "externalNull" to null and string scalar returns null
    [InlineData("nonNullInput", null, "internalNull", Response.ErrorNoData)]      // custom scalar fails validation on null for non-null argument before execution begins
    public void InOut_Literal(string field, string argumentValue, string expectedResponse, Response responseType)
    {
        var expectedResult = new ExecutionResult();
        if (responseType == Response.Success || responseType == Response.Error)
        {
            expectedResult.Data = new Dictionary<string, object>
            {
                { field, expectedResponse },
            };
            expectedResult.Executed = true;
        }
        else if (responseType == Response.ErrorDataNull)
        {
            expectedResult.Data = null;
            expectedResult.Executed = true;
        }

        var quotedArg = argumentValue == null ? "null" : $"\"{argumentValue}\"";
        var actualResult = AssertQueryIgnoreErrors($"{{ {field}(arg: {quotedArg}) }}", expectedResult,
            expectedErrorCount: responseType == Response.Success ? 0 : 1);
        if (responseType == Response.ErrorDataNull || responseType == Response.Error)
        {
            actualResult.Errors[0].Path.ShouldBe(new object[] { field });
        }
    }

    [Theory]
    [InlineData("input", "hello", "hello", "CustomScalar", Response.Success)]                     // custom scalar receives "hello" and string scalar returns "hello"
    [InlineData("input", null, "internalNull", "CustomScalar", Response.Success)]                 // custom scalar coerces null to "internalNull" and string scalar returns "internalNull"
    [InlineData("input", "externalNull", null, "CustomScalar", Response.Success)]                 // custom scalar coerces "externalNull" to null and string scalar returns null
    [InlineData("input", "error", null, "CustomScalar", Response.ErrorNoData)]                    // custom scalar throws while coercing "error", fails document validation
    [InlineData("output", "hello", "hello", "String", Response.Success)]                          // string scalar receives "hello" and custom scalar returns "hello"
    [InlineData("output", null, "externalNull", "String", Response.Success)]                      // string scalar receives null and custom scalar returns "externalNull"
    [InlineData("output", "internalNull", null, "String", Response.Success)]                      // string scalar receives "internalNull" and custom scalar returns it as null
    [InlineData("output", "error", null, "String", Response.Error)]                               // string scalar receives "error" and custom scalar throws attempting to convert it
    [InlineData("inputOutput", "hello", "hello", "CustomScalar", Response.Success)]               // custom scalar receives "hello" and then custom scalar returns "hello"
    [InlineData("inputOutput", null, null, "CustomScalar", Response.Success)]                     // custom scalar coerces null to "internalNull" and then custom scalar coerces "internalNull" to null
    [InlineData("inputOutput", "internalNull", null, "CustomScalar", Response.Success)]           // custom scalar receives "internalNull" and then custom scalar coerces "internalNull" to null
    [InlineData("inputOutput", "externalNull", "externalNull", "CustomScalar", Response.Success)] // custom scalar coerces "externalNull" to null and then custom scalar coerces null to "externalNull"
    [InlineData("nonNullOutput", "internalNull", null, "String", Response.ErrorDataNull)]         // string scalar receives "internalNull" and non-null custom scalar attempts to return null (and fails)
    [InlineData("nonNullOutput", null, "externalNull", "String", Response.Success)]               // string scalar receives null and non-null custom scalar returns "externalNull" for non-null field
    [InlineData("nonNullInput", "externalNull", null, "CustomScalar!", Response.Success)]         // custom scalar coerces "externalNull" to null and string scalar returns null
    [InlineData("nonNullInput", null, "internalNull", "CustomScalar!", Response.ErrorNoData)]     // custom scalar fails validation on null for non-null argument before execution begins
    public void InOut_Variable(string field, string argumentValue, string expectedResponse, string argumentType, Response responseType)
    {
        var expectedResult = new ExecutionResult();
        if (responseType == Response.Success || responseType == Response.Error)
        {
            expectedResult.Data = new Dictionary<string, object>
            {
                { field, expectedResponse },
            };
            expectedResult.Executed = true;
        }
        else if (responseType == Response.ErrorDataNull)
        {
            expectedResult.Data = null;
            expectedResult.Executed = true;
        }

        var quotedArg = argumentValue == null ? "null" : $"\"{argumentValue}\"";
        var actualResult = AssertQueryIgnoreErrors($"query ($arg: {argumentType}) {{ {field}(arg: $arg) }}", expectedResult, $"{{ \"arg\": {quotedArg} }}".ToInputs(),
            expectedErrorCount: responseType == Response.Success ? 0 : 1);
        if (responseType == Response.ErrorDataNull || responseType == Response.Error)
        {
            actualResult.Errors[0].Path.ShouldBe(new object[] { field });
        }
    }

    [Fact]
    public void List_works()
    {
        // verify that within lists, custom scalars are coerced to their proper values
        AssertQuerySuccess("{ list }", @"{ ""list"": [""hello"", null, ""externalNull"" ]}");
    }

    [Fact]
    public void List_NonNull_works_valid()
    {
        // here we are using a custom scalar to coerce a null value to a non-null value, which is valid for a non-null type
        AssertQuerySuccess("{ listNonNullValid }", @"{ ""listNonNullValid"": [""hello"", ""externalNull"" ]}");
    }

    [Fact]
    public void List_NonNull_works_invalid()
    {
        // verify that within lists of non-null values, errors are returned properly
        var query = "{ listNonNullInvalid }";
        var response = @"{ ""listNonNullInvalid"": null}";
        var result = AssertQueryWithErrors(query, response, expectedErrorCount: 1);
        var errorIndex = 1;
        // index should be 1 here because custom scalar will convert ["hello", "internalNull"] to ["hello", null]
        result.Errors.ShouldNotBeNull();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Message.ShouldBe("Error trying to resolve field 'listNonNullInvalid'.");
        result.Errors[0].Path.ShouldBe(new object[] { "listNonNullInvalid", errorIndex });
    }
}

public enum Response
{
    /// <summary>
    /// Field is set properly
    /// </summary>
    Success,
    /// <summary>
    /// Field is set to null due to execution error within the field on a nullable field
    /// </summary>
    Error,
    /// <summary>
    /// Data is null due to execution error within the field on a non-null field
    /// </summary>
    ErrorDataNull,
    /// <summary>
    /// Data does not exist in the response map due to a validation error before execution begins
    /// </summary>
    ErrorNoData
}

public class CustomScalarSchema : Schema
{
    public CustomScalarSchema()
    {
        Query = new CustomScalarQuery();
    }
}

public class CustomScalarQuery : ObjectGraphType
{
    public CustomScalarQuery()
    {
        Field(typeof(StringGraphType), "input",
            arguments: new QueryArguments { new QueryArgument(typeof(CustomScalar)) { Name = "arg" } },
            resolve: context => context.GetArgument<string>("arg"));
        Field(typeof(CustomScalar), "output",
            arguments: new QueryArguments { new QueryArgument(typeof(StringGraphType)) { Name = "arg" } },
            resolve: context => context.GetArgument<string>("arg"));
        Field(typeof(CustomScalar), "inputOutput",
            arguments: new QueryArguments { new QueryArgument(typeof(CustomScalar)) { Name = "arg" } },
            resolve: context => context.GetArgument<string>("arg"));
        Field(typeof(StringGraphType), "nonNullInput",
            arguments: new QueryArguments { new QueryArgument(typeof(NonNullGraphType<CustomScalar>)) { Name = "arg" } },
            resolve: context => context.GetArgument<string>("arg"));
        Field(typeof(NonNullGraphType<CustomScalar>), "nonNullOutput",
            arguments: new QueryArguments { new QueryArgument(typeof(StringGraphType)) { Name = "arg" } },
            resolve: context => context.GetArgument<string>("arg"));
        Field(typeof(ListGraphType<CustomScalar>), "list",
            resolve: context => new object[] { "hello", "internalNull", null });
        Field(typeof(ListGraphType<NonNullGraphType<CustomScalar>>), "listNonNullValid",
            resolve: context => new object[] { "hello", null });
        Field(typeof(ListGraphType<NonNullGraphType<CustomScalar>>), "listNonNullInvalid",
            resolve: context => new object[] { "hello", "internalNull" });
    }
}

public class CustomScalar : ScalarGraphType
{
    // semantics:
    //
    // EXTERNAL           INTERNAL
    // ----------------------------------------
    // "externalNull"     null
    // null               "internalNull"
    // "hello"            "hello"
    //
    // Attempting to parse or serialize "error" results in an exception

    public override object ParseValue(object value)
    {
        if (value as string == "externalNull")
            return null;

        if (value == null)
            return "internalNull";

        if (value as string == "error")
            throw new Exception("Cannot parse value");

        return value.ToString();
    }

    public override object ParseLiteral(GraphQLValue value)
    {
        if (value is GraphQLStringValue stringValue)
        {
            if (stringValue.Value == "externalNull")
                return null;

            if (stringValue.Value == "error")
                throw new Exception("Cannot parse value");

            return (string)stringValue.Value;
        }

        if (value is GraphQLNullValue)
        {
            return "internalNull";
        }

        throw new NotSupportedException();
    }

    public override object Serialize(object value)
    {
        if (value == null)
            return "externalNull";

        if (value as string == "internalNull")
            return null;

        if (value as string == "error")
            throw new Exception("Cannot serialize value");

        return value.ToString();
    }
}
