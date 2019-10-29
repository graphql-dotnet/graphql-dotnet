using System;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.DI;
using System.Threading.Tasks;
using System.ComponentModel;

namespace GraphQL.StarWars
{
    [Name("Query")]
    public class StarWarsQueryDI : DIObjectGraphBase
    {
        private StarWarsData _data;

        public StarWarsQueryDI(StarWarsData data)
        {
            _data = data;
        }

        [GraphType(typeof(CharacterInterface))]
        public async Task<StarWarsCharacter> Hero() => await _data.GetDroidByIdAsync("3");

        [GraphType(typeof(HumanType))]
        public async Task<Human> Human([Required] [Description("id of the human")] string id) => await _data.GetHumanByIdAsync(id);

        [GraphType(typeof(DroidTypeDIGraph))]
        public async Task<Droid> Droid([Required] [Description("id of the droid")] string id) => await _data.GetDroidByIdAsync(id);

    }
}
