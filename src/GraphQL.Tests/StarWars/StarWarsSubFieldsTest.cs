using System.Collections.Generic;
using GraphQL.Types;
using Shouldly;
using Xunit;
using GraphQL.StarWars.Types;
using GraphQL.StarWars;

namespace GraphQL.Tests.StarWars
{
    public class StarWarsSubFieldsTests : StarWarsTestBase
    {
        public StarWarsSubFieldsTests() : base()
        {
            this.RootQuery = (StarWarsQuery)this.Schema.Query;
        }

        public StarWarsQuery RootQuery;

        [Fact]
        public void subfields_is_not_null_for_ListGraphType()
        {
            RootQuery.FieldAsync<ListGraphType<HumanType>>("listOfHumans", resolve: async (ctx) =>
            {
                //this test fails
                ctx.SubFields.ShouldNotBeNull();
                ctx.SubFields.Keys.ShouldContain("id");
                ctx.SubFields.Keys.ShouldContain("friends");
                return new List<Human>();
            });
            var query = @"
                {
                    listOfHumans {
                        id
                        friends {
                            name
                        }
                    }
                }
            ";

            var expected = @"
                {
                    listOfHumans: []
                }
            ";
            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void subfields_is_not_null_for_custom_graph_type()
        {
            RootQuery.Field<HumanType>("singleHuman", resolve: (ctx) =>
            {
                ctx.SubFields.ShouldNotBeNull();
                ctx.SubFields.Keys.ShouldContain("id");
                ctx.SubFields.Keys.ShouldContain("friends");
                return null;
            });

            var query = @"
                {
                    singleHuman {
                        id
                        friends {
                            name
                        }
                    }
                }
            ";
            var expected = @"
                {
                    singleHuman: null
                }
            ";
            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void subfields_does_not_throw_for_primative()
        {
            RootQuery.Field<IntGraphType>("someNumber", resolve: (ctx) =>
            {
                ctx.SubFields.ShouldBeNull();
                return 1;
            });

            var query = @"
                {
                    someNumber
                }
            ";
            var expected = @"
                {
                    someNumber: 1
                }
            ";
            AssertQuerySuccess(query, expected);
        }

        [Fact]
        public void subfields_does_not_throw_for_list_of_primative()
        {
            RootQuery.Field<ListGraphType<IntGraphType>>("someNumbers", resolve: (ctx) =>
            {
                ctx.SubFields.ShouldBeNull();
                return new int[] { 1, 2 };
            });

            var query = @"
                {
                    someNumbers
                }
            ";
            var expected = @"
                {
                    someNumbers: [1,2]
                }
            ";
            AssertQuerySuccess(query, expected);
        }

    }
}
