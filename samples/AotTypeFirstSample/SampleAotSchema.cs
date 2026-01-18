using GraphQL.DI;
using GraphQL.Types;

namespace AotSample;

//[AutoQueryType(typeof(StarWarsQuery))] // will discover all related type-first types
// other attributes available: [AutoMutationType], [AutoSubscriptionType], [AutoOutputType], [AutoInputType], [HasType]
public partial class SampleAotSchema : AotSchema
{
    // ctor is available for developer's use, and code runs after construction in generated Configure method
    public SampleAotSchema(IServiceProvider services, IEnumerable<IConfigureSchema> configurations) : base(services, configurations)
    {
    }
}
