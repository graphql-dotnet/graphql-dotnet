using System.Collections.Generic;

namespace GraphQL.Tests
{
    public class DroidType : ObjectGraphType
    {
        public DroidType()
        {
            var data = new StarWarsData();

            Name = "Droid";
            Fields = new List<FieldType>
            {
                new FieldType
                {
                    Name = "id",
                    Description = "The id of the droid.",
                    Type = new NonNullGraphType(new StringGraphType())
                },
                new FieldType
                {
                    Name = "name",
                    Description = "The name of the droid.",
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