using GraphQL.Types;

namespace AotSample;

//[AotQueryType(typeof(StarWarsQuery))] // will discover all related type-first types
// other attributes available: [AotMutationType], [AotSubscriptionType], [AotOutputType], [AotInputType], [AotGraphType], [AotInterfaceType], [AotUnionType], [AotTypeMapping]
public partial class SampleAotSchema : AotSchema
{
}
