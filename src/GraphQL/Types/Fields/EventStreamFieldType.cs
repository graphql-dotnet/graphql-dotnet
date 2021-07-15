using GraphQL.Resolvers;

namespace GraphQL.Types
{
    public class EventStreamFieldType : FieldType
    {
        public IEventStreamResolver? Subscriber { get; set; }

        public IAsyncEventStreamResolver? AsyncSubscriber { get; set; }
    }
}
