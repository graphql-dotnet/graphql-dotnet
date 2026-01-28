using GraphQL;
using GraphQL.StarWars.TypeFirst.Types;
using GraphQL.Types;

namespace AotSample;

[AotQueryType<StarWarsQuery>] // will discover related types
[AotMutationType<StarWarsMutation>]
// other attributes available: [AotSubscriptionType], [AotOutputType], [AotInputType], [AotGraphType], [AotTypeMapping]
public partial class SampleAotSchema : AotSchema
{
}
