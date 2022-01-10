using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Tests
{
    public class GraphQLSerializersTestData : IEnumerable<object[]>
    {
        public static readonly List<IGraphQLSerializer> AllWriters = new List<IGraphQLSerializer>
        {
            new SystemTextJson.GraphQLSerializer(new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // less strict about what is encoded into \uXXXX
            }),
            new NewtonsoftJson.GraphQLSerializer(settings =>
            {
                settings.Formatting = Newtonsoft.Json.Formatting.Indented;
                settings.Converters.Add(new NewtonsoftJson.FixPrecisionConverter(true, true, true));
            })
        };

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var writer in AllWriters)
            {
                yield return new object[] { writer };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
