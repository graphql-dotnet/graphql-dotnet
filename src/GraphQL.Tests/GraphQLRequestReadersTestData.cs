using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Tests
{
    public class GraphQLRequestReadersTestData : IEnumerable<object[]>
    {
        public static readonly List<IGraphQLRequestReader> AllReaders = new List<IGraphQLRequestReader>
        {
            new SystemTextJson.GraphQLRequestReader(),
            //new NewtonsoftJson.GraphQLRequestReader(),
        };

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var writer in AllReaders)
            {
                yield return new object[] { writer };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
