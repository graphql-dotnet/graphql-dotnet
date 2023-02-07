using GraphQL.Federation.Extensions;
using GraphQL.Types;

namespace GraphQL.Federation.Tests.Schema.External;

public sealed class ExternalResolvableTestType : AutoRegisteringObjectGraphType<ExternalResolvableTestDto>
{
    public ExternalResolvableTestType()
    {
        this.ResolveReference((ctx, rep) => rep);

        Field<NonNullGraphType<StringGraphType>>("Extended")
            .Resolve(ctx => ctx.Source.External)
            .Requires(nameof(ExternalResolvableTestDto.External));
    }
}
