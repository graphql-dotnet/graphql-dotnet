using System;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.DI;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;

namespace GraphQL.StarWars
{
    [Name("Query")]
    public class StarWarsQueryDI : DIObjectGraphBase
    {
        private StarWarsData _data; //this could be a scoped service

        public StarWarsQueryDI(StarWarsData data)
        {
            _data = data;
        }

        [GraphType(typeof(CharacterInterface))] //not required when using GraphTypeTypeRegistry
        public async Task<StarWarsCharacter> Hero() => await _data.GetDroidByIdAsync("3");

        [GraphType(typeof(HumanType))] //not required when using GraphTypeTypeRegistry
        public async Task<Human> HumanAsync([Required] [Description("id of the human")] string id, CancellationToken token) => await _data.GetHumanByIdAsync(id, token);

        [GraphType(typeof(DroidTypeDIGraph))] //not required when using GraphTypeTypeRegistry
        public async Task<Droid> Droid([Required] [Description("id of the droid")] string id) => await _data.GetDroidByIdAsync(id);

    }
}
