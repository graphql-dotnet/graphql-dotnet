namespace GraphQL.Tests.Execution;

[Collection("StaticTests")]
public class InputConversionSystemTextJsonTests : InputConversionTestsBase
{
    private static readonly IGraphQLTextSerializer _serializer = new SystemTextJson.GraphQLSerializer(indent: true);
    protected override Inputs VariablesToInputs(string variables)
        => _serializer.Deserialize<Inputs>(variables) ?? Inputs.Empty;
}
