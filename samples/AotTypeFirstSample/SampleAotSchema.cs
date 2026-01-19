using GraphQL;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;

namespace AotSample;

[AotQueryType<StarWarsQuery>] // will discover related types
// other attributes available: [AotMutationType], [AotSubscriptionType], [AotOutputType], [AotInputType], [AotGraphType], [AotTypeMapping]
public partial class SampleAotSchema : AotSchema
{
}
