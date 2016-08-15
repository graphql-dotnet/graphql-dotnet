using System.ComponentModel;
using GraphQL.SchemaGenerator.Attributes;

namespace GraphQL.SchemaGenerator.Tests
{
    [GraphType]
    public class EchoStateSchema
    {
        private static int EchoState { get; set; } = 1;

        [Description(@"Sets the state.")]
        [GraphRoute(isMutation:true)]
        public SchemaResponse SetState(int request)
        {
            EchoState = request;

            return GetState();
        }

        [Description(@"Reads the state.")]
        [GraphRoute] //since it returns a value, query will be assumed
        public SchemaResponse GetState()
        {
            return new SchemaResponse()
            {
                Value = EchoState
            };
        }
    }
}
