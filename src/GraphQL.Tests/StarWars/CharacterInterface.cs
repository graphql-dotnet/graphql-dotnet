using GraphQL.Types;

namespace GraphQL.Tests
{
    public class CharacterInterface : InterfaceGraphType
    {
        public CharacterInterface()
        {
            Name = "Character";
            Field("id", "The id of the character.", NonNullGraphType.String);
            Field("name", "The name of the character.", ScalarGraphType.String);
            Field("friends", new ListGraphType<CharacterInterface>());
            Field("appearsIn", "Which movie they appear in.", new ListGraphType<EpisodeEnum>());

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
