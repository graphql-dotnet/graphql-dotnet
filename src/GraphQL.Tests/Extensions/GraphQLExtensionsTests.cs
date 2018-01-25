using System;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Extensions
{
    public class GraphQLExtensionsTests
    {
        [Fact]
        void BuildNamedType_ResolveReturnNull_Throws()
        {
            Should.Throw<InvalidOperationException>(
                () => typeof(ListGraphType<ListGraphType<EpisodeEnum>>).BuildNamedType(_ => null));
        }
    }
}
