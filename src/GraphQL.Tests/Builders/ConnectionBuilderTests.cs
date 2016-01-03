using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Builders
{
    public class ConnectionBuilderTests : QueryTestBase<ConnectionBuilderTests.TestSchema>
    {
        public class ParentType : ObjectGraphType
        {
            public ParentType()
            {
                Name = "Parent";

                Connection<ChildType, Child>()
                    .Name("connection1")
                    .WithObject<Parent>()
                    .Unidirectional()
                    .Resolve(context => context.Object.Connection1);

                Connection<ChildType, Child>()
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
                Connection1 = new List<Child>
                {
                    new Child { Field1 = "one", Field2 = 1 },
                    new Child { Field1 = "two", Field2 = 2 },
                    new Child { Field1 = "three", Field2 = 3 },
                };
                Connection2 = new List<Child>
                {
                    new Child { Field1 = "ONE", Field2 = 11 },
                    new Child { Field1 = "TWO", Field2 = 22 },
                    new Child { Field1 = "THREE", Field2 = 33 },
                };
            }

            public List<Child> Connection1 { get; set; }

            public List<Child> Connection2 { get; set; }
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

        [Test]
        public void ConnectionWithName()
        {
            var objectType = new ParentType();
            objectType.Fields.First().Name.ShouldEqual("connection1");
        }

        [Test]
        public void ConnectionWithDescription()
        {
            var objectType = new ParentType();
            objectType.Fields.ElementAt(0).Description.ShouldEqual(null);
            objectType.Fields.ElementAt(1).Description.ShouldEqual("RandomDescription");
        }

        [Test]
        public void ConnectionMetaData()
        {
            var connectionType = new ConnectionType<ChildType>();
            connectionType.Name.ShouldEqual("ChildConnection");
            connectionType.Description.ShouldEqual("A connection from an object to a list of objects of type `Child`.");
        }

        [Test]
        public void ConnectionsInSchema()
        {
            AssertQuerySuccess(
                "{ parent {" +
                "   connection1 { totalCount edges { cursor node { field1 field2 } } items { field1 field2 } }" +
                "   connection2 { totalCount edges { cursor node { field1 field2 } } items { field1 field2 } }" +
                "}",
                "{ parent: {" +
                "connection1: {" +
                "  totalCount: 3," +
                "  edges: [" +
                "    { cursor: '00000001', node: { field1: 'one', field2: 1 } }," +
                "    { cursor: '00000002', node: { field1: 'two', field2: 2 } }," +
                "    { cursor: '00000003', node: { field1: 'three', field2: 3 } }" +
                "  ]," +
                "  items: [" +
                "    { field1: 'one', field2: 1 }," +
                "    { field1: 'two', field2: 2 }," +
                "    { field1: 'three', field2: 3 }" +
                "  ]" +
                "}," +
                "connection2: {" +
                "  totalCount: 3," +
                "  edges: [" +
                "    { cursor: '00000001', node: { field1: 'ONE', field2: 11 } }," +
                "    { cursor: '00000002', node: { field1: 'TWO', field2: 22 } }," +
                "    { cursor: '00000003', node: { field1: 'THREE', field2: 33 } }" +
                "  ]," +
                "  items: [" +
                "    { field1: 'ONE', field2: 11 }," +
                "    { field1: 'TWO', field2: 22 }," +
                "    { field1: 'THREE', field2: 33 }" +
                "  ]" +
                "}" +
                "} }");
        }

        [Test]
        public void ConnectionsInSchemaWithPagination()
        {
            AssertQuerySuccess(
                "{ parent {" +
                "   connection1(first: 1, after: \"00000001\") { totalCount edges { cursor node { field1 field2 } } items { field1 field2 } }" +
                "   connection2(last: 1) { totalCount edges { cursor node { field1 field2 } } items { field1 field2 } }" +
                "}",
                "{ parent: {" +
                "connection1: {" +
                "  totalCount: 3," +
                "  edges: [" +
                "    { cursor: '00000002', node: { field1: 'two', field2: 2 } }" +
                "  ]," +
                "  items: [" +
                "    { field1: 'two', field2: 2 }" +
                "  ]" +
                "}," +
                "connection2: {" +
                "  totalCount: 3," +
                "  edges: [" +
                "    { cursor: '00000003', node: { field1: 'THREE', field2: 33 } }" +
                "  ]," +
                "  items: [" +
                "    { field1: 'THREE', field2: 33 }" +
                "  ]" +
                "}" +
                "} }");
        }
    }
}
