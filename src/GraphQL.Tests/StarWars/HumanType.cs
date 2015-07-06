using System.Collections.Generic;

namespace GraphQL.Tests
{
    public class HumanType : ObjectGraphType
    {
        public HumanType()
        {
            var data = new StarWarsData();

            Name = "Human";
            Fields = new List<FieldType>
            {
                new FieldType
                {
                    Name = "id",
                    Description = "The id of the human.",
                    Type = new NonNullGraphType(new StringGraphType())
                },
                new FieldType
                {
                    Name = "name",
                    Description = "The name of the human.",
                    Type = new NonNullGraphType(new StringGraphType())
                },
                new FieldType
                {
                    Name = "friends",
                    Type = new ListGraphType(typeof(CharacterInterface)),
                    Resolve = (context) =>
                    {
                        return data.GetFriends(context.Source as StarWarsCharacter);
                    }
                }
            };
            Interfaces = new List<InterfaceGraphType>
            {
                new CharacterInterface()
            };
        }
    }
}