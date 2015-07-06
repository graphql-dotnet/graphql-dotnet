using System.Collections.Generic;

namespace GraphQL.Tests
{
    public class StarWarsQuery : ObjectGraphType
    {
        public StarWarsQuery()
        {
            var data = new StarWarsData();

            Fields = new List<FieldType>
            {
                new FieldType
                {
                    Name = "hero",
                    Type = new CharacterInterface(),
                    Resolve = (obj) => data.GetDroidById("3")
                },
                new FieldType
                {
                    Name = "human",
                    Type = new HumanType(),
                    Arguments = new QueryArguments(new List<QueryArgument>
                    {
                        new QueryArgument { Name = "id", Type = new NonNullGraphType(new StringGraphType())}
                    }),
                    Resolve = (context) => data.GetHumanById((string)context.Arguments["id"])
                },
                new FieldType
                {
                    Name = "droid",
                    Type = new DroidType(),
                    Arguments = new QueryArguments(new List<QueryArgument>
                    {
                        new QueryArgument { Name = "id", Type = new NonNullGraphType(new StringGraphType())}
                    }),
                    Resolve = (context) => data.GetDroidById((string)context.Arguments["id"])
                }
            };
        }
    }
}