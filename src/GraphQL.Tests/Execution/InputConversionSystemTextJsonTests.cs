namespace GraphQL.Tests.Execution;

[Collection("StaticTests")]
public class InputConversionSystemTextJsonTests : InputConversionTestsBase
{
    protected override Inputs VariablesToInputs(string variables) => variables.ToInputs();
}
