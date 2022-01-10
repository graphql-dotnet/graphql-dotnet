using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Tests
{
    public class GraphQLSerializersDefaultTestData : IEnumerable<object[]>
    {
        public static readonly List<IGraphQLTextSerializer> AllWriters = new List<IGraphQLTextSerializer>
        {
            new SystemTextJson.GraphQLSerializer(),
            new NewtonsoftJson.GraphQLSerializer()
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
