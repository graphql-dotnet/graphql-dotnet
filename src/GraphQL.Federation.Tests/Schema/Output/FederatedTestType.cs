using GraphQL.DataLoader;
using GraphQL.Federation.Extensions;
using GraphQL.Federation.Tests.Schema.External;
using GraphQL.Types;

namespace GraphQL.Federation.Tests.Schema.Output;

public sealed class FederatedTestType : AutoRegisteringObjectGraphType<FederatedTestDto>
{
    public FederatedTestType(IDataLoaderContextAccessor accessor)
    {
        // this.ResolveReference((ctx, rep) => new() { Id = rep.Id, Name = $"{rep.Id}{rep.Id}{rep.Id}", ExternalTestId = 4 - rep.Id });
        this.ResolveReference((ctx, rep) => accessor.Context.GetOrAddBatchLoader<int, FederatedTestDto>(
            $"{nameof(FederatedTestDto)}-ResolveReference",
            (items) =>
            {
                return Task.FromResult<IDictionary<int, FederatedTestDto>>(new Dictionary<int, FederatedTestDto>
                {
                    [1] = new() { Id = 1, Name = "111", ExternalTestId = 4, ExternalResolvableTestId = 7 },
                    [2] = new() { Id = 2, Name = "222", ExternalTestId = 5, ExternalResolvableTestId = 8 },
                    [3] = new() { Id = 3, Name = "333", ExternalTestId = 6, ExternalResolvableTestId = 9 },
                });
            }).LoadAsync(rep.Id));

        Field<NonNullGraphType<GraphQLClrOutputTypeReference<ExternalTestDto>>>("ExternalTest")
            .Resolve(ctx => new ExternalTestDto
            {
                Id = ctx.Source.ExternalTestId
            })
            .DeprecationReason("Test deprecation reason 02.");

        Field<NonNullGraphType<GraphQLClrOutputTypeReference<ExternalResolvableTestDto>>>("ExternalResolvableTest")
            .Resolve(ctx => new ExternalResolvableTestDto
            {
                Id = ctx.Source.ExternalResolvableTestId,
                External = $"external-{ctx.Source.ExternalResolvableTestId}"
            })
            .Provides(nameof(ExternalResolvableTestDto.External));
    }
}
