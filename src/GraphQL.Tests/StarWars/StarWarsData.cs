using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Tests
{
    public class StarWarsData
    {
        private readonly List<Human> _humans = new List<Human>();
        private readonly List<Droid> _droids = new List<Droid>();

        public StarWarsData()
        {
            _humans.Add(new Human { Id = "1", Name = "Luke"});
            _humans.Add(new Human { Id = "2", Name = "Vader"});

            _droids.Add(new Droid { Id = "3", Name = "R2-D2", Friends = new []{"1", "4"}});
            _droids.Add(new Droid { Id = "4", Name = "C-3PO"});
        }

        public IEnumerable<StarWarsCharacter> GetFriends(StarWarsCharacter character)
        {
            if (character == null)
            {
                return null;
            }

            var friends = new List<StarWarsCharacter>();
            var lookup = character.Friends;
            if (lookup != null)
            {
                _humans.Where(h => lookup.Contains(h.Id)).Apply(friends.Add);
                _droids.Where(d => lookup.Contains(d.Id)).Apply(friends.Add);
            }
            return friends;
        }

        public Human GetHumanById(string id)
        {
            return _humans.FirstOrDefault(h => h.Id == id);
        }

        public Droid GetDroidById(string id)
        {
            return _droids.FirstOrDefault(h => h.Id == id);
        }
    }
}