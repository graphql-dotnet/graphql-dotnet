using GraphQL.Types;

namespace GraphQL.Tests.Bugs.Configuration
{
    public class AdvancedSchemaTestBase : QueryTestBase<AdvancedSchema>
    {
        public AdvancedSchemaTestBase()
        {
            Services.Register<ResultType>();
            Services.Register<AdvancedQuery>();
            Services.Register<AdvancedEnumType>();
            
            Services.Singleton(() => new AdvancedSchema(type => (GraphType) Services.Get(type)));
        }
    }
}
