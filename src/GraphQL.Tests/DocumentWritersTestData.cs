using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Tests
{
    public class DocumentWritersTestData : IEnumerable<object[]>
    {
        public static readonly List<IDocumentWriter> AllWriters = new List<IDocumentWriter>
        {
            new SystemTextJson.DocumentWriter(new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // less strict about what is encoded into \uXXXX
            }),
            new NewtonsoftJson.DocumentWriter(settings =>
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
