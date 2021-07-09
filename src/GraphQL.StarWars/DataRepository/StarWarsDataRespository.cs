using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.StarWars.Types;

namespace GraphQL.StarWars.DataRepository
{
    public class StarWarsDataRespository : IStarWarsDataRespository
    {
        private static readonly List<StarWarsCharacter> _characters = new()
        {
            new Human
            {
                Id = "1",
                Name = "Luke",
                Friends = new List<string> { "3", "4" },
                AppearsIn = new[] { 4, 5, 6 },
                HomePlanet = "Tatooine",
                Cursor = "MQ=="
            },
            new Human
            {
                Id = "2",
                Name = "Vader",
                AppearsIn = new[] { 4, 5, 6 },
                HomePlanet = "Tatooine",
                Cursor = "Mg=="
            },
            new Droid
            {
                Id = "3",
                Name = "R2-D2",
                Friends = new List<string> { "1", "4" },
                AppearsIn = new[] { 4, 5, 6 },
                PrimaryFunction = "Astromech",
                Cursor = "Mw=="
            },
            new Droid
            {
                Id = "4",
                Name = "C-3PO",
                AppearsIn = new[] { 4, 5, 6 },
                PrimaryFunction = "Protocol",
                Cursor = "NA=="
            }
        };

        public IEnumerable<StarWarsCharacter> GetFriends(StarWarsCharacter character)
        {
            if (character == null)
            {
                return null;
            }

            var lookup = character.Friends;
            if (lookup == null)
            {
                return Enumerable.Empty<StarWarsCharacter>();
            }

            var friends = _characters.Where(h => lookup.Contains(h.Id));
            return friends;
        }

        public StarWarsCharacter AddCharacter(StarWarsCharacter character)
        {
            character.Id = _characters.Count.ToString();
            _characters.Add(character);

            return character;
        }

        public Task<Human> GetHumanByIdAsync(string id)
        {
            var match = _characters.FirstOrDefault(h => h.Id == id);
            var human = match is Human asHuman ? asHuman : null;

            return Task.FromResult(human);
        }

        public Task<Droid> GetDroidByIdAsync(string id)
        {
            var match = _characters.FirstOrDefault(h => h.Id == id);
            var droid = match is Droid asHuman ? asHuman : null;

            return Task.FromResult(droid);
        }

        public Task<List<StarWarsCharacter>> GetCharactersAsync(List<string> guids)
        {
            var results = _characters.Where(c => guids.Contains(c.Id)).ToList();

            return Task.FromResult(results);
        }

        public async Task<List<StarWarsCharacter>> GetAllCharactersAsync()
        {
            var results = await Task.FromResult(_characters.ToList());

            return results;
        }
    }
}
