namespace GraphQL.Tests.Execution;

public class InputConversionSystemTextJsonTests : InputConversionTestsBase
{
    protected override Inputs VariablesToInputs(string variables) => variables.ToInputs();
}
