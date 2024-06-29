using GraphQL.Federation.Tests.Schema.Output;
using GraphQL.Types;

namespace GraphQL.Federation.Tests.Schema;

public class TestQuery : ObjectGraphType
{
    public TestQuery()
    {
        // this.AddServices();
        // this.AddEntities();

        Field<NonNullGraphType<GraphQLClrOutputTypeReference<DirectivesTestDto>>>("directivesTest")
            .Resolve(context => new DirectivesTestDto());
    }
}
