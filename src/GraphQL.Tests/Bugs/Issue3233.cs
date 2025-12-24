using GraphQL.Execution;

namespace GraphQL.Tests.Bugs;

public class Issue3233
{
    [Fact]
    public void Throws_NotSupportedException_On_Object_Key()
    {
        var serializer = new SystemTextJson.GraphQLSerializer(new ErrorInfoProvider(opt => opt.ExposeData = true));
        var result = new ExecutionResult();
        result.AddError(new ExecutionError("oops1"));
        string serialized = serializer.Serialize(result);
        serialized.ShouldBe("{\"errors\":[{\"message\":\"oops1\"}]}");

        result = new ExecutionResult();
        var errorWithData = new ExecutionError("oops2");
        errorWithData.Data.Add(new object(), "WOW");
        result.AddError(errorWithData);

        var ex = Should.Throw<NotSupportedException>(() => serializer.Serialize(result));
        string[] messages = new[]
        {
            "The type 'System.Object' is not a supported Dictionary key type. Path: $.",
            "The type 'System.Object' is not a supported dictionary key using converter of type 'System.Text.Json.Serialization.Converters.ObjectConverter'. Path: $.",
            "The type 'System.Object' is not a supported dictionary key using converter of type 'System.Text.Json.Serialization.Converters.DefaultObjectConverter'. Path: $.",
            "The collection type 'System.Collections.ListDictionaryInternal' is not supported.",
            "The type 'System.Object' is not a supported dictionary key using converter of type 'System.Text.Json.Serialization.Converters.DefaultObjectConverter'. Custom converters can add support for dictionary key serialization by overriding the 'ReadAsPropertyName' and 'WriteAsPropertyName' methods. Path: $."
        };
        if (!messages.Contains(ex.Message))
            throw ex;
    }
}
