namespace GraphQL.Tests.Execution;

public class InputConversionNewtonsoftJsonTests : InputConversionTestsBase
{
    protected override Inputs VariablesToInputs(string variables) => variables.ToInputs();
}
