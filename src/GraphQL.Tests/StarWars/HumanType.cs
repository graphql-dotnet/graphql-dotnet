using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Tests
{
    public class HumanType : ObjectGraphType
    {
        public async Task<object> GetFriendsAsync(StarWarsData data, StarWarsCharacter source)
        {
            Debug.WriteLine("GetFriendsAsync: Start (" + source.Id + ")");
            await Task.Delay(1000);
            Debug.WriteLine("GetFriendsAsync: Stop (" + source.Id + ")");
            return data.GetFriends(source);
        }

        public HumanType()
        {
            var data = new StarWarsData();

            Name = "Human";

            Field<NonNullGraphType<StringGraphType>>("id", "The id of the human.");
            Field<StringGraphType>("name", "The name of the human.");
            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: (context) =>
                {
                    return GetFriendsAsync(data, context.Source as StarWarsCharacter);
                    // return data.GetFriends(context.Source as StarWarsCharacter);
                }
            );
            Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
            Field<StringGraphType>("homePlanet", "The home planet of the human.");

            Interface<CharacterInterface>();
        }
    }
}
