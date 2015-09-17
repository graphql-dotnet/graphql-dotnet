using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Tests
{
    public class DroidType : ObjectGraphType
    {
        public async Task<object> GetFriendsAsync(StarWarsData data, StarWarsCharacter source)
        {
            Debug.WriteLine("GetFriendsAsync: Start (" + source.Id + ")");
            await Task.Delay(1000);
            Debug.WriteLine("GetFriendsAsync: Stop (" + source.Id + ")");
            return data.GetFriends(source);
        }

        public DroidType()
        {
            var data = new StarWarsData();

            Name = "Droid";
            Description = "A mechanical creature in the Star Wars universe.";

            Field<NonNullGraphType<StringGraphType>>("id", "The id of the droid.");
            Field<StringGraphType>("name", "The name of the droid.");
            Field<ListGraphType<CharacterInterface>>(
                "friends",
                resolve: (context) =>
                {
                    return GetFriendsAsync(data, context.Source as StarWarsCharacter);
                }
            );
            Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
            Field<StringGraphType>("primaryFunction", "The primary function of the droid.");

            Interface<CharacterInterface>();
        }
    }
}
