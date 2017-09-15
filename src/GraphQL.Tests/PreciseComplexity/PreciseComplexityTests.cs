using Shouldly;
using System;
using Xunit;

namespace GraphQL.Tests.PreciseComplexity
{
    public class PreciseComplexityTests : PreciseComplexityTestBase
    {
        [Fact]
        public void scalar_field_query()
        {
            var query = "{string}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d);
            result.MaxDepth.ShouldBe(1);
        }

        [Fact]
        public void array_field_query()
        {
            var query = "{stringList}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d); // treating scalar arrays as scalar
            result.MaxDepth.ShouldBe(1);
        }

        [Fact]
        public void two_depth_query()
        {
            var query = "{this {string}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d + 1d);
            result.MaxDepth.ShouldBe(2);
        }

        [Fact]
        public void two_depth_array_query()
        {
            var query = "{thisList {string}}";
            var result = this.Analyze(query);  
            result.Complexity.ShouldBe(1d + 10d * 1d);
            result.MaxDepth.ShouldBe(2);
        }

        [Fact]
        public void connection_query()
        {
            var query = "{connection(limit: 5) {string}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d + 5d);
            result.MaxDepth.ShouldBe(2);
        }

        [Fact]
        public void connection_with_variables_query()
        {
            var query = "query test($limit:Int){connection(limit: $limit) {string}}";
            var variables = "{\"limit\": 7}";
            var result = this.Analyze(query, variables);
            result.Complexity.ShouldBe(1d + 7d);
            result.MaxDepth.ShouldBe(2);
        }

        [Fact]
        public void three_depth_array_with_array_query()
        {
            var query = "{thisList {thisList {string}}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d + 10d * (1d + 10d * 1d) );
            result.MaxDepth.ShouldBe(3);
        }

        [Fact]
        public void three_depth_query()
        {
            var query = "{this {this {string}}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d + 1d + 1d);
            result.MaxDepth.ShouldBe(3);
        }

        [Fact]
        public void fragment_inline_query()
        {
            var query = "{this {...on RootQuery{ string }}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d + 1d);
            result.MaxDepth.ShouldBe(2);
        }

        [Fact]
        public void fragment_double_inline_query()
        {
            var query = "{this {...on RootQuery{ string }, ...on RootQuery { string }}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d + 1d + 1d); // complexity analyzer doesn't try to optimize query
            result.MaxDepth.ShouldBe(2);
        }

        [Fact]
        public void fragment_query()
        {
            var query = "{this {...F}} fragment F on RootQuery { string }";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d + 1d);
            result.MaxDepth.ShouldBe(2);
        }

        [Fact]
        public void fragment_double_query()
        {
            var query = "{this {...F1, ...F2}} fragment F1 on RootQuery { string } fragment F2 on RootQuery { string }";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d + 1d + 1d); // complexity analyzer doesn't try to optimize query
            result.MaxDepth.ShouldBe(2);
        }

        [Fact]
        public void interface_query()
        {
            var query = "{this {...on RootInterface{ this { string } }}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d + 1d + 1d);
            result.MaxDepth.ShouldBe(3);
        }

        [Fact]
        public void interface_invalid_query()
        {
            var query = "{this {...on NonRootInterface{ this { int } }}}";
            var result = this.Analyze(query);
            result.Complexity.ShouldBe(1d + 1d + 1d); // complexity analyzer doesn't check interface or type compatibility and will caclulate assuming that all fragments will run
            result.MaxDepth.ShouldBe(3);
        }

        [Fact]
        public void complexity_fail()
        {
            var query = "{this { this { this { string } } } }";
            this.Analyze(query, maxComplexity: 4d);
            Assert.Throws(typeof(InvalidOperationException), () => this.Analyze(query, maxComplexity: 3));
        }

        [Fact]
        public void max_depth_fail()
        {
            var query = "{this { this { this { string } } } }";
            this.Analyze(query, maxDepth: 4);
            Assert.Throws(typeof(InvalidOperationException), () => this.Analyze(query, maxDepth: 3));
        }
    }
}
