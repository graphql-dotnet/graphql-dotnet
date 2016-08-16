using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using GraphQL.SchemaGenerator.Attributes;

namespace GraphQL.SchemaGenerator.Tests
{
    [GraphType]
    public class EchoStateSchema
    {
        private static StateResponse State { get; } = new StateResponse();

        [Description(@"Sets the data.")]
        [GraphRoute(isMutation:true)]
        public StateResponse SetData(int request)
        {
            State.Data = request;

            return GetState();
        }

        [Description(@"Sets both the data and state.")]
        [GraphRoute(isMutation: true)]
        public StateResponse Set(SetRequest request)
        {
            State.Data = request.Data;
            State.State = request.State ?? ValidStates.Open;
            State.Decimal = request.Decimal;

            return GetState();
        }

        [Description(@"Sets the state.")]
        [GraphRoute(isMutation: true)]
        public StateResponse SetState(ValidStates request)
        {
            State.State = request;

            return GetState();
        }

        [Description(@"Reads the state.")]
        [GraphRoute] //since it returns a value, query will be assumed
        public StateResponse GetState()
        {
            return State;
        }
    }

    public enum ValidStates
    {
        Open = 1,
        Closed = 0
    };

    public class StateResponse
    {
        public ValidStates State { get; set; }
        public decimal? Decimal { get; set; }
        public int Data { get; set; }
    }

    public class SetRequest
    {
        public ValidStates? State { get; set; }

        public decimal? Decimal { get; set; }

        [Required]
        public int Data { get; set; }
    }
}
