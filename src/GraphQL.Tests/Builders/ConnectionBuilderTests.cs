using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Types.Relay.DataObjects;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Builders
{
    public class ConnectionBuilderTests : QueryTestBase<ConnectionBuilderTests.TestSchema>
    {
        [Fact]
        public void should_have_name()
        {
            var objectType = new ParentType();
            objectType.Fields.First().Name.ShouldBe("connection1");
        }

        [Fact]
        public void should_have_optional_description()
        {
            var objectType = new ParentType();
            objectType.Fields.ElementAt(0).Description.ShouldBe(null);
            objectType.Fields.ElementAt(1).Description.ShouldBe("RandomDescription");
        }

        [Fact]
        public void should_have_optional_deprecation_reason()
        {
            var objectType = new ParentType();
            objectType.Fields.ElementAt(0).DeprecationReason.ShouldBe("Deprecated");
            objectType.Fields.ElementAt(1).DeprecationReason.ShouldBe(null);
        }

        [Fact]
        public void should_have_field_information()
        {
            var connectionType = new ConnectionType<ChildType>();
            connectionType.Name.ShouldBe("ChildConnection");
            connectionType.Description.ShouldBe("A connection from an object to a list of objects of type `Child`.");
        }

        [Fact]
        public void should_resolve_in_query()
        {
            AssertQuerySuccess(
                @"{ parent {
                  connection1 {
                    totalCount
                    edges { cursor node { field1 field2 } }
                    items { field1 field2 }
                  }
                }}",
                @"{ parent: {
                connection1: {
                  totalCount: 3,
                  edges: [
                    { cursor: '1', node: { field1: 'one', field2: 1 } },
                    { cursor: '2', node: { field1: 'two', field2: 2 } },
                    { cursor: '3', node: { field1: 'three', field2: 3 } }
                  ],
                  items: [
                    { field1: 'one', field2: 1 },
                    { field1: 'two', field2: 2 },
                    { field1: 'three', field2: 3 }
                  ]
                } } }");
        }

        [Fact]
        public void should_yield_pagination_information()
        {
            AssertQuerySuccess(
                @"{ parent {
                  connection2(first: 1, after: ""1"") {
                    totalCount
                    pageInfo { hasNextPage hasPreviousPage startCursor endCursor }
                    edges { cursor node { field1 field2 } }
                    items { field1 field2 }
                  } }}",
                @"{ parent: {
                connection2: {
                  totalCount: 3,
                  pageInfo: {
                    hasNextPage: true,
                    hasPreviousPage: true,
                    startCursor: '2',
                    endCursor: '2',
                  },
                  edges: [
                    { cursor: '2', node: { field1: 'TWO', field2: 22 } }
                  ],
                  items: [
                    { field1: 'TWO', field2: 22 }
                  ]
                } } }");
        }

        [Fact]
        public void can_define_simple_connection_with_resolver()
        {
            var type = new ObjectGraphType();

            type.Connection<ObjectGraphType>()
                .Name("testConnection")
                .Resolve(resArgs =>
                    new Connection<Child>
                    {
                        TotalCount = 1,
                        PageInfo = new PageInfo
                        {
                            HasNextPage = true,
                            HasPreviousPage = false,
                            StartCursor = "01",
                            EndCursor = "01",
                        },
                        Edges = new List<Edge<Child>>
                        {
                            new Edge<Child>
                            {
                                Cursor = "01",
                                Node = new Child
                                {
                                    Field1 = "abcd",
                                },
                            },
                        },
                    });

            var field = type.Fields.Single();
            field.Name.ShouldBe("testConnection");
            field.Type.ShouldBe(typeof(ConnectionType<ObjectGraphType>));

            var result = field.Resolver.Resolve(new ResolveFieldContext()) as Connection<Child>;

            result.ShouldNotBeNull();
            if (result != null)
            {
                result.TotalCount.ShouldBe(1);
                result.PageInfo.HasNextPage.ShouldBe(true);
                result.PageInfo.HasPreviousPage.ShouldBe(false);
                result.PageInfo.StartCursor.ShouldBe("01");
                result.PageInfo.EndCursor.ShouldBe("01");
                result.Edges.Count.ShouldBe(1);
                result.Edges.First().Cursor.ShouldBe("01");
                result.Edges.First().Node.Field1.ShouldBe("abcd");
                result.Items.Count.ShouldBe(1);
                result.Items.First().Field1.ShouldBe("abcd");
            }
        }

        public class ParentType : ObjectGraphType<Parent>
        {
            public ParentType()
            {
                Name = "Parent";

                Connection<ChildType>()
                    .Name("connection1")
                    .Unidirectional()
                    .DeprecationReason("Deprecated")
                    .Resolve(context => context.Source.Connection1);

                Connection<ChildType>()
                    .Name("connection2")
                    .Description("RandomDescription")
                    .Bidirectional()
                    .Resolve(context => context.Source.Connection2);
            }
        }

        public class ChildType : ObjectGraphType<Child>
        {
            public ChildType()
            {
                Name = "Child";

                Field<StringGraphType>()
                    .Name("field1");

                Field<IntGraphType>()
                    .Name("field2");
            }
        }

        public class Parent
        {
            public Parent()
            {
                Connection1 = new Connection<Child>
                {
                    TotalCount = 3,
                    PageInfo = new PageInfo
                    {
                        StartCursor = "1",
                        EndCursor = "3",
                    },
                    Edges = new List<Edge<Child>>
                    {
                        new Edge<Child> { Cursor = "1", Node = new Child { Field1 = "one", Field2 = 1 } },
                        new Edge<Child> { Cursor = "2", Node = new Child { Field1 = "two", Field2 = 2 } },
                        new Edge<Child> { Cursor = "3", Node = new Child { Field1 = "three", Field2 = 3 } },
                    }
                };
                Connection2 = new Connection<Child>
                {
                    TotalCount = 3,
                    PageInfo = new PageInfo
                    {
                        HasNextPage = true,
                        HasPreviousPage = true,
                        StartCursor = "2",
                        EndCursor = "2",
                    },
                    Edges = new List<Edge<Child>>
                    {
                        new Edge<Child> { Cursor = "2", Node = new Child { Field1 = "TWO", Field2 = 22 } },
                    }
                };
            }

            public Connection<Child> Connection1 { get; set; }

            public Connection<Child> Connection2 { get; set; }
        }

        public class Child
        {
            public string Field1 { get; set; }

            public int Field2 { get; set; }
        }

        public class TestQuery : ObjectGraphType
        {
            public TestQuery()
            {
                Name = "Query";

                Field<ParentType>()
                    .Name("parent")
                    .Resolve(_ => new Parent());
            }
        }

        public class TestSchema : Schema
        {
            public TestSchema()
            {
                Query = new TestQuery();
            }
        }
    }
}
