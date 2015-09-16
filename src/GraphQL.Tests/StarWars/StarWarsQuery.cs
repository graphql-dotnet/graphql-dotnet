using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Tests
{
    public class StarWarsQuery : ObjectGraphType
    {

        private async Task<object> GetDroidByIdAsync(string id)
        {            
            Debug.WriteLine("GetDroidByIdAsync: Start (" + id + ")");
            var data = new StarWarsData();
            await Task.Delay(5000);
            Debug.WriteLine("GetDroidByIdAsync: Stop (" + id + ")");
            return data.GetDroidById(id);
        }

        private async Task<object> GetHumanByIdAsync(string id)
        {
            Debug.WriteLine("GetHumanByIdAsync: Start (" + id + ")");
            var data = new StarWarsData();
            await Task.Delay(5000);
            Debug.WriteLine("GetHumanByIdAsync: Stop (" + id + ")");
            return data.GetHumanById(id);
        } 

        public StarWarsQuery()
        {
            var data = new StarWarsData();

            Name = "Query";

            Field<CharacterInterface>("hero", 
                resolve: (context) => {
                    return GetDroidByIdAsync("3");
                }
            );
            Field<HumanType>(
                "human",
                arguments: new QueryArguments(
                    new []
                    {
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the human" }
                    }),
                resolve: (context) =>
                {
                    return GetHumanByIdAsync((string)context.Arguments["id"]);
                    // return data.GetHumanById((string)context.Arguments["id"]);
                }
            );
            Field<DroidType>(
                "droid",
                arguments: new QueryArguments(
                    new[]
                    {
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the droid" }
                    }),
                resolve: (context) =>
                {
                    return GetDroidByIdAsync((string)context.Arguments["id"]);
                    // data.GetDroidById((string)context.Arguments["id"]);
                }
            );
        }
    }
}
