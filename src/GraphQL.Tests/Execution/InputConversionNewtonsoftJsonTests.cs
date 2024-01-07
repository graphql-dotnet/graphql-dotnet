namespace GraphQL.Tests.Execution;

[Collection("StaticTests")]
public class InputConversionNewtonsoftJsonTests : InputConversionTestsBase
{
    private static readonly IGraphQLTextSerializer _serializer = new NewtonsoftJson.GraphQLSerializer(indent: true);
    protected override Inputs VariablesToInputs(string? variables)
        => _serializer.Deserialize<Inputs>(variables) ?? Inputs.Empty;
}
