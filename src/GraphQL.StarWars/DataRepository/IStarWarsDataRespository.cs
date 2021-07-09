using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.StarWars.Types;

namespace GraphQL.StarWars.DataRepository
{
    public interface IStarWarsDataRespository
    {
        IEnumerable<StarWarsCharacter> GetFriends(StarWarsCharacter character);

        StarWarsCharacter AddCharacter(StarWarsCharacter character);

        Task<Human> GetHumanByIdAsync(string id);

        Task<Droid> GetDroidByIdAsync(string id);

        Task<List<StarWarsCharacter>> GetCharactersAsync(List<string> guids);
    }
}
