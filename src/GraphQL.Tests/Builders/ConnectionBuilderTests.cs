using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Types.Relay.DataObjects;
using Should;

namespace GraphQL.Tests.Builders
{
    public class ConnectionBuilderTests : QueryTestBase<ConnectionBuilderTests.TestSchema>
    {
        [Fact]
        public void should_have_name()
        {
            var objectType = new ParentType();
            objectType.Fields.First().Name.ShouldEqual("connection1");
        }

        [Fact]
        public void should_have_optional_description()
        {
            var objectType = new ParentType();
            objectType.Fields.ElementAt(0).Description.ShouldEqual(null);
            objectType.Fields.ElementAt(1).Description.ShouldEqual("RandomDescription");
        }

        [Fact]
        public void should_have_optional_deprecation_reason()
        {
            var objectType = new ParentType();
            objectType.Fields.ElementAt(0).DeprecationReason.ShouldEqual("Deprecated");
            objectType.Fields.ElementAt(1).DeprecationReason.ShouldEqual(null);
        }

        [Fact]
        public void should_have_field_information()
        {
            var connectionType = new ConnectionType<ChildType>();
            connectionType.Name.ShouldEqual("ChildConnection");
            connectionType.Description.ShouldEqual("A connection from an object to a list of objects of type `Child`.");
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
            field.Name.ShouldEqual("testConnection");
            field.Type.ShouldEqual(typeof(ConnectionType<ObjectGraphType>));
            var result = field.Resolve(null) as Connection<Child>;

            result.ShouldNotBeNull();
            if (result != null)
            {
                result.TotalCount.ShouldEqual(1);
                result.PageInfo.HasNextPage.ShouldEqual(true);
                result.PageInfo.HasPreviousPage.ShouldEqual(false);
                result.PageInfo.StartCursor.ShouldEqual("01");
                result.PageInfo.EndCursor.ShouldEqual("01");
                result.Edges.Count.ShouldEqual(1);
                result.Edges.First().Cursor.ShouldEqual("01");
                result.Edges.First().Node.Field1.ShouldEqual("abcd");
                result.Items.Count.ShouldEqual(1);
                result.Items.First().Field1.ShouldEqual("abcd");
            }
        }

        public class ParentType : ObjectGraphType
        {
            public ParentType()
            {
                Name = "Parent";

                Connection<ChildType>()
                    .Name("connection1")
                    .WithObject<Parent>()
                    .Unidirectional()
                    .DeprecationReason("Deprecated")
                    .Resolve(context => context.Object.Connection1);

                Connection<ChildType>()
                    .Name("connection2")
                    .Description("RandomDescription")
                    .WithObject<Parent>()
                    .Bidirectional()
                    .Resolve(context => context.Object.Connection2);
            }
        }

        public class ChildType : ObjectGraphType
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
                    .Returns<Parent>()
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
