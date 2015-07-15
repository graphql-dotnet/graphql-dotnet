using GraphQL.Types;

namespace GraphQL.Tests
{
    public class StarWarsSchema : Schema
    {
        public StarWarsSchema()
        {
            Query = new StarWarsQuery();
        }
    }
}
