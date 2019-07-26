using GraphQL.StarWars.Types;
using GraphQL.Types;
using Shouldly;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        [Fact]
        void GetResult_Extension_Should_Work()
        {
            var task1 = Task.FromResult(42);
            task1.GetResult().ShouldBe(42);

            var obj = new object();
            var task2 = Task.FromResult(obj);
            task2.GetResult().ShouldBe(obj);

            IEnumerable collection = new List<string>();
            var task3 = Task.FromResult(collection);
            task3.GetResult().ShouldBe(collection);

            ILookup<string, EqualityComparer<DateTime>> lookup = new List<EqualityComparer<DateTime>>().ToLookup(i => i.GetHashCode().ToString());
            var task4 = Task.FromResult(lookup);
            task4.GetResult().ShouldBe(lookup);
        }
    }
}
