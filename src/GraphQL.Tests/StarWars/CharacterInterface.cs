using System.Collections.Generic;

namespace GraphQL.Tests
{
    public class CharacterInterface : InterfaceGraphType
    {
        public CharacterInterface()
        {
            Name = "Character";
            Fields = new List<FieldType>
            {
                new FieldType
                {
                    Name = "id",
                    Description = "The id of the character.",
                    Type = new NonNullGraphType(new StringGraphType())
                },
                new FieldType
                {
                    Name = "name",
                    Description = "The name of the character.",
                    Type = new NonNullGraphType(new StringGraphType())
                },
                new FieldType
                {
                    Name = "friends",
                    Type = new ListGraphType(typeof(CharacterInterface))
                }
            };
            ResolveType = (obj) =>
            {
                if (obj is Human)
                {
                    return new HumanType();
                }

                return new DroidType();
            };
        }
    }
}