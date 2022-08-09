using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Types.Relay.DataObjects;

namespace GraphQL.Tests.Builders;

public class ConnectionBuilderTests : QueryTestBase<ConnectionBuilderTests.TestSchema>
{
    [Fact]
    public void should_have_name()
    {
        var objectType = new ParentType();
        objectType.Fields.First().Name.ShouldBe("connection1");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void should_throw_error_if_name_is_null_or_empty(string fieldName)
    {
        var type = new ObjectGraphType();
        ArgumentOutOfRangeException exception;
        // race condition with does_not_throw_with_filtering_nameconverter test
        try
        {
            exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Connection<ObjectGraphType>().Name(fieldName));
        }
        catch (ShouldAssertException)
        {
            System.Threading.Thread.Sleep(100); // wait a bit and retry
            exception = Should.Throw<ArgumentOutOfRangeException>(() => type.Connection<ObjectGraphType>().Name(fieldName));
        }

        exception.Message.ShouldStartWith("A field name can not be null or empty.");
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
            @"{ ""parent"": {
                ""connection1"": {
                  ""totalCount"": 3,
                  ""edges"": [
                    { ""cursor"": ""1"", ""node"": { ""field1"": ""one"", ""field2"": 1 } },
                    { ""cursor"": ""2"", ""node"": { ""field1"": ""two"", ""field2"": 2 } },
                    { ""cursor"": ""3"", ""node"": { ""field1"": ""three"", ""field2"": 3 } }
                  ],
                  ""items"": [
                    { ""field1"": ""one"", ""field2"": 1 },
                    { ""field1"": ""two"", ""field2"": 2 },
                    { ""field1"": ""three"", ""field2"": 3 }
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
            @"{ ""parent"": {
                ""connection2"": {
                  ""totalCount"": 3,
                  ""pageInfo"": {
                    ""hasNextPage"": true,
                    ""hasPreviousPage"": true,
                    ""startCursor"": ""2"",
                    ""endCursor"": ""2""
                  },
                  ""edges"": [
                    { ""cursor"": ""2"", ""node"": { ""field1"": ""TWO"", ""field2"": 22 } }
                  ],
                  ""items"": [
                    { ""field1"": ""TWO"", ""field2"": 22 }
                  ]
                } } }");
    }

    [Fact]
    public async Task can_define_simple_connection_with_resolver()
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
        field.Type.ShouldBe(typeof(ConnectionType<ObjectGraphType, EdgeType<ObjectGraphType>>));

        var result = await field.Resolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false) as Connection<Child>;

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

    [Fact]
    public async Task can_define_simple_connection_with__async_resolver()
    {
        var type = new ObjectGraphType();
        var connection = new Connection<Child>
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
        };
        type.Connection<ObjectGraphType>()
            .Name("testConnection")
            .ResolveAsync(resArgs => Task.FromResult<object>(connection));

        var field = type.Fields.Single();
        field.Name.ShouldBe("testConnection");
        field.Type.ShouldBe(typeof(ConnectionType<ObjectGraphType, EdgeType<ObjectGraphType>>));

        var boxedResult = await field.Resolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false);
        var result = boxedResult as Connection<Child>;

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

    [Fact]
    public async Task can_define_simple_connection_with__custom_edge_type()
    {
        var type = new ObjectGraphType();
        var connection = new Connection<Child, ParentChildrenEdge>
        {
            TotalCount = 1,
            PageInfo = new PageInfo
            {
                HasNextPage = true,
                HasPreviousPage = false,
                StartCursor = "01",
                EndCursor = "01",
            },
            Edges = new List<ParentChildrenEdge>
            {
                new ParentChildrenEdge
                {
                    Cursor = "01",
                    Node = new Child
                    {
                        Field1 = "abcd",
                    },
                    FriendedAt = FriendedAt
                },
            },
        };
        type.Connection<ChildType, ParentChildrenEdgeType>()
            .Name("testConnection")
            .ResolveAsync(resArgs => Task.FromResult<object>(connection));

        var field = type.Fields.Single();
        field.Name.ShouldBe("testConnection");
        field.Type.ShouldBe(typeof(ConnectionType<ChildType, ParentChildrenEdgeType>));

        var boxedResult = await field.Resolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false);
        var result = boxedResult as Connection<Child, ParentChildrenEdge>;

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
            result.Edges.ShouldAllBe(c => c.FriendedAt == FriendedAt);
        }
    }

    [Fact]
    public async Task can_define_simple_connection_with__custom_edge_and_connection_types()
    {
        var type = new ObjectGraphType();
        var connection = new ParentChildrenConnection
        {
            TotalCount = 1,
            PageInfo = new PageInfo
            {
                HasNextPage = true,
                HasPreviousPage = false,
                StartCursor = "01",
                EndCursor = "01",
            },
            Edges = new List<ParentChildrenEdge>
            {
                new ParentChildrenEdge
                {
                    Cursor = "01",
                    Node = new Child
                    {
                        Field1 = "abcd",
                        Field2 = 1
                    },
                    FriendedAt = FriendedAt
                },
                new ParentChildrenEdge
                {
                    Cursor = "01",
                    Node = new Child
                    {
                        Field1 = "abcd",
                        Field2 = 10
                    },
                    FriendedAt = FriendedAt
                },
                new ParentChildrenEdge
                {
                    Cursor = "01",
                    Node = new Child
                    {
                        Field1 = "abcd",
                        Field2 = 7
                    },
                    FriendedAt = FriendedAt
                },
            },
            ConnectionField1 = ConnectionField1Value
        };
        type.Connection<ChildType, ParentChildrenEdgeType, ParentChildrenConnectionType>()
            .Name("testConnection")
            .ResolveAsync(resArgs => Task.FromResult<object>(connection));

        var field = type.Fields.Single();
        field.Name.ShouldBe("testConnection");
        field.Type.ShouldBe(typeof(ParentChildrenConnectionType));

        var boxedResult = await field.Resolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false);
        var result = boxedResult as ParentChildrenConnection;

        result.ShouldNotBeNull();
        if (result != null)
        {
            result.TotalCount.ShouldBe(1);
            result.PageInfo.HasNextPage.ShouldBe(true);
            result.PageInfo.HasPreviousPage.ShouldBe(false);
            result.PageInfo.StartCursor.ShouldBe("01");
            result.PageInfo.EndCursor.ShouldBe("01");
            result.Edges.Count.ShouldBe(3);
            result.Edges.First().Cursor.ShouldBe("01");
            result.Edges.First().Node.Field1.ShouldBe("abcd");
            result.Items.Count.ShouldBe(3);
            result.Items.First().Field1.ShouldBe("abcd");
            result.Edges.ShouldAllBe(c => c.FriendedAt == FriendedAt);
            result.HighestField2.ShouldBe(10);
            result.ConnectionField1.ShouldBe(ConnectionField1Value);
        }
    }

    [Fact]
    public void unidirectional_creates_proper_arguments()
    {
        var graph = new ParentType();
        graph.Fields.Find("connection1").Arguments.Count(x => x.Name == "after").ShouldBe(1);
        graph.Fields.Find("connection1").Arguments.Count(x => x.Name == "first").ShouldBe(1);
        graph.Fields.Find("connection1").Arguments.Count(x => x.Name == "before").ShouldBe(0);
        graph.Fields.Find("connection1").Arguments.Count(x => x.Name == "last").ShouldBe(0);
    }

    [Fact]
    public void bidirectional_creates_proper_arguments()
    {
        var graph = new ParentType();
        graph.Fields.Find("connection2").Arguments.Count(x => x.Name == "after").ShouldBe(1);
        graph.Fields.Find("connection2").Arguments.Count(x => x.Name == "first").ShouldBe(1);
        graph.Fields.Find("connection2").Arguments.Count(x => x.Name == "before").ShouldBe(1);
        graph.Fields.Find("connection2").Arguments.Count(x => x.Name == "last").ShouldBe(1);
    }

    [Fact]
    public void bidirectional_called_twice_creates_proper_arguments()
    {
        var graph = new ObjectGraphType();
        graph.Connection<ChildType>()
            .Name("connection")
            .Description("RandomDescription")
            .Bidirectional()
            .Bidirectional();

        graph.Fields.Find("connection").Arguments.Count(x => x.Name == "before").ShouldBe(1);
        graph.Fields.Find("connection").Arguments.Count(x => x.Name == "last").ShouldBe(1);
    }

    [Fact]
    public async Task should_use_pagesize()
    {
        var graph = new ObjectGraphType();
        graph.Connection<ChildType>()
            .Name("connection")
            .PageSize(10)
            .Resolve(context => context.First);
        (await graph.Fields.Find("connection").Resolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false)).ShouldBe(10);
    }

    [Fact]
    public async Task should_use_pagesize_async()
    {
        var graph = new ObjectGraphType();
        graph.Connection<ChildType>()
            .Name("connection")
            .PageSize(10)
            .ResolveAsync(context => Task.FromResult<object>(context.First));
        (await graph.Fields.Find("connection").Resolver.ResolveAsync(new ResolveFieldContext()).ConfigureAwait(false)).ShouldBe(10);
    }

    public class ParentChildrenConnection : Connection<Child, ParentChildrenEdge>
    {
        public int? HighestField2 => Edges?.Max(e => e.Node?.Field2);

        public int ConnectionField1 { get; set; }
    }

    public class ParentChildrenConnectionType : ConnectionType<ChildType, ParentChildrenEdgeType>
    {
        public ParentChildrenConnectionType()
        {
            Field<NonNullGraphType<IntGraphType>>("highestField2")
                .Description("The highest value of all Child's Field2 values in current page of the connection.");

            Field<NonNullGraphType<IntGraphType>>("connectionField1")
                .Description("An example of a manually set field on the connection.");
        }
    }

    public class ParentChildrenEdge : Edge<Child>
    {
        public DateTime FriendedAt { get; set; }
    }

    public class ParentChildrenEdgeType : EdgeType<ChildType>
    {
        public ParentChildrenEdgeType()
        {
            Field<NonNullGraphType<DateTimeGraphType>>("friendedAd")
                .Description("When parent became friend with child.");
        }
    }

    private const int ConnectionField1Value = 123;
    private static readonly DateTime FriendedAt = new(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public class ParentType : ObjectGraphType<Parent>
    {
        public ParentType()
        {
            Name = "Parent";

            Connection<ChildType>()
                .Name("connection1")
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

            Field<StringGraphType>("field1");

            Field<IntGraphType>("field2");
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

            Field<ParentType>("parent")
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
