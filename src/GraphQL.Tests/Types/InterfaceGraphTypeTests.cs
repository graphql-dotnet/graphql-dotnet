using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Types;

public class InterfaceGraphTypeTests : QueryTestBase<InterfaceGraphTypeTests.MySchema>
{
    [Fact]
    public void VerifyArgumentsWork()
    {
        var query = """
            {
                test {
                    hello(name: "John Doe")
                }
            }
            """;
        var expected = """
            {
                "test": {
                    "hello": "John Doe"
                }
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void VerifyDefaultArgumentsWork()
    {
        var query = """
            {
                test {
                    hello
                }
            }
            """;
        var expected = """
            {
                "test": {
                    "hello": "world"
                }
            }
            """;

        AssertQuerySuccess(query, expected);
    }

    [Fact]
    public void SupportsInterfaceInheritance_SchemaFirst()
    {
        var sdl = """
            interface Node {
              id: ID!
            }

            interface Resource implements Node {
              id: ID!
              url: String
            }

            interface Image implements Resource & Node {
              id: ID!
              url: String
              thumbnail: String
            }

            type Picture implements Image & Resource & Node {
              id: ID!
              url: String
              thumbnail: String
              favorite: Boolean
            }

            type Query {
              img: Picture
            }
            """;
        var schema = GraphQL.Types.Schema.For(sdl);
        ValidateInterfaceInheritance(schema);
    }

    [Fact]
    public void SupportsInterfaceInheritance_CodeFirst1()
    {
        // Define Node interface
        var nodeInterface = new InterfaceGraphType() { Name = "Node" };
        nodeInterface.Field<NonNullGraphType<IdGraphType>>("id");

        // Define Resource interface and add Node as a resolved interface
        var resourceInterface = new InterfaceGraphType() { Name = "Resource" };
        resourceInterface.Field<NonNullGraphType<IdGraphType>>("id");
        resourceInterface.Field<StringGraphType>("url");
        resourceInterface.AddResolvedInterface(nodeInterface);

        // Define Image interface and add Resource and Node as resolved interfaces
        var imageInterface = new InterfaceGraphType() { Name = "Image" };
        imageInterface.Field<NonNullGraphType<IdGraphType>>("id");
        imageInterface.Field<StringGraphType>("url");
        imageInterface.Field<StringGraphType>("thumbnail");
        imageInterface.AddResolvedInterface(resourceInterface);
        imageInterface.AddResolvedInterface(nodeInterface);

        // Define Picture type and implement Image, Resource, and Node interfaces
        var pictureType = new ObjectGraphType() { Name = "Picture" };
        pictureType.Field<NonNullGraphType<IdGraphType>>("id");
        pictureType.Field<StringGraphType>("url");
        pictureType.Field<StringGraphType>("thumbnail");
        pictureType.Field<BooleanGraphType>("favorite");
        pictureType.AddResolvedInterface(imageInterface);
        pictureType.AddResolvedInterface(resourceInterface);
        pictureType.AddResolvedInterface(nodeInterface);

        // Define Query type with a field returning a Picture
        var queryType = new ObjectGraphType() { Name = "Query" };
        queryType.Field("img", pictureType);

        // Create schema
        var schema = new Schema
        {
            Query = queryType
        };
        schema.RegisterType(nodeInterface);
        schema.RegisterType(resourceInterface);
        schema.RegisterType(imageInterface);

        // Validate schema
        ValidateInterfaceInheritance(schema);
    }

    [Fact]
    public void SupportsInterfaceInheritance_CodeFirst2()
    {
        var schema = new MySchema2();
        schema.Initialize();
        ValidateInterfaceInheritance(schema);
    }

    [Fact]
    public void SupportsInterfaceInheritance_TypeFirst()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<TypeFirstSchema.Query>());
        var provider = services.BuildServiceProvider();
        var schema = provider.GetService<ISchema>()!;
        schema.Initialize();
        ValidateInterfaceInheritance(schema);
    }

    [Fact]
    public void ReturnInterfacesRequiresResolver()
    {
        var schema = new MySchema2();
        // the schema can be initialized without this field because no object graph type returns the interface type Image.
        // here, since a field has been added that returns the interface type Image, the interface type must either
        //   provide a resolveType function, or each possible type must provide an IsTypeOf function.
        schema.Query.Field("test", new GraphQLTypeReference("Image"));
        Should.Throw<InvalidOperationException>(schema.Initialize)
            .Message.ShouldBe("Interface type 'Image' does not provide a 'resolveType' function and possible Type 'Picture' does not provide a 'isTypeOf' function.  There is no way to resolve this possible type during execution.");
    }

    [Fact]
    public async Task SupportsInterfaceInheritance_Introspection()
    {
        var schema = new MySchema2();
        schema.Initialize();
        var result = await schema.ExecuteAsync(
            o => o.Query = """
                {
                  __type(name: "Image") {
                    name
                    kind
                    interfaces {
                      name
                      kind
                    }
                  }
                }
                """);
        result.ShouldBe("""
            {
              "data": {
                "__type": {
                  "name": "Image",
                  "kind": "INTERFACE",
                  "interfaces": [
                    {
                      "name": "Resource",
                      "kind": "INTERFACE"
                    },
                    {
                      "name": "Node",
                      "kind": "INTERFACE"
                    }
                  ]
                }
              }
            }
            """, StringCompareShould.IgnoreLineEndings);
    }

    private void ValidateInterfaceInheritance(ISchema schema)
    {
        schema.Initialize();
        var printedSdl = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase });
        printedSdl.ShouldBe("""
            interface Image implements Node & Resource {
              id: ID!
              thumbnail: String
              url: String
            }

            interface Node {
              id: ID!
            }

            type Picture implements Image & Node & Resource {
              favorite: Boolean
              id: ID!
              thumbnail: String
              url: String
            }

            type Query {
              img: Picture
            }

            interface Resource implements Node {
              id: ID!
              url: String
            }
            """, StringCompareShould.IgnoreLineEndings);
    }

    [Theory]
    [InlineData(11, typeof(Interface1), typeof(Interface2), false)]
    [InlineData(12, typeof(Interface2), typeof(Interface1), false)]
    [InlineData(13, typeof(Interface2), typeof(Interface3), true)]
    [InlineData(14, typeof(Interface3), typeof(Interface2), false)]
    [InlineData(15, typeof(Interface6), typeof(Interface7), true)]
    [InlineData(16, typeof(Interface7), typeof(Interface6), false)]
    [InlineData(17, typeof(Interface8), typeof(Interface9), true)]
    [InlineData(18, typeof(Interface9), typeof(Interface8), false)]
    [InlineData(19, typeof(Interface10), typeof(Interface8), true)]
    [InlineData(20, typeof(Interface8), typeof(Interface10), false)]
    [InlineData(21, typeof(Interface10), typeof(Interface9), false)]
    [InlineData(22, typeof(Interface9), typeof(Interface10), false)]
    [InlineData(23, typeof(Interface8), typeof(Interface11), true)]
    [InlineData(24, typeof(Interface11), typeof(Interface8), false)]
    private void IsValidInterfaceFor(int i, Type interfaceType, Type superType, bool isValidInterface)
    {
        _ = i;
        var schema = new Schema() { Query = new ObjectGraphType() };
        schema.Query.AddField(new FieldType { Name = "dummy", Type = typeof(StringGraphType) });
        schema.RegisterType(superType);
        schema.RegisterType(interfaceType);
        schema.Initialize();
        var superGraphType = schema.AllTypes[((IGraphType)Activator.CreateInstance(superType)!).Name].ShouldBeAssignableTo<IComplexGraphType>().ShouldNotBeNull();
        var interfaceGraphType = schema.AllTypes[((IGraphType)Activator.CreateInstance(interfaceType)!).Name].ShouldBeAssignableTo<IInterfaceGraphType>().ShouldNotBeNull();
        interfaceGraphType.IsValidInterfaceFor(superGraphType, false).ShouldBe(isValidInterface);
    }

    public class MySchema : Schema
    {
        public MySchema()
        {
            Query = new MyQuery();
            this.RegisterType<MyObject>();
        }
    }

    public class MyQuery : ObjectGraphType
    {
        public MyQuery()
        {
            Field<MyInterface>("test")
                .Resolve(_ => 123);
        }
    }

    public class MyInterface : InterfaceGraphType
    {
        public MyInterface()
        {
            Field<string>("hello", true)
                .Argument<string>("name", true, arg => arg.DefaultValue = "world")
                .DefaultValue("world");
        }
    }

    public class MyObject : ObjectGraphType<int>
    {
        public MyObject()
        {
            Interface<MyInterface>();
            Field<string>("hello", true)
                .Argument<string>("name", true, arg => arg.DefaultValue = "world")
                .Resolve(ctx => ctx.GetArgument<string>("name"));
        }
    }

    public class MySchema2 : Schema
    {
        public MySchema2()
        {
            Query = new QueryType();
        }

        public class QueryType : ObjectGraphType
        {
            public QueryType()
            {
                Name = "Query";

                Field<PictureGraphType>("img");
            }
        }

        public class PictureGraphType : ObjectGraphType
        {
            public PictureGraphType()
            {
                Name = "Picture";

                Interface<ImageGraphType>();
                Interface<ResourceGraphType>();
                Interface<NodeGraphType>();

                Field<NonNullGraphType<IdGraphType>>("id");
                Field<StringGraphType>("url");
                Field<StringGraphType>("thumbnail");
                Field<BooleanGraphType>("favorite");
            }
        }

        public class ImageGraphType : InterfaceGraphType
        {
            public ImageGraphType()
            {
                Name = "Image";

                Interface<ResourceGraphType>();
                Interface<NodeGraphType>();

                Field<NonNullGraphType<IdGraphType>>("id");
                Field<StringGraphType>("url");
                Field<StringGraphType>("thumbnail");
            }
        }

        public class ResourceGraphType : InterfaceGraphType
        {
            public ResourceGraphType()
            {
                Name = "Resource";

                Interface<NodeGraphType>();

                Field<NonNullGraphType<IdGraphType>>("id");
                Field<StringGraphType>("url");
            }
        }

        public class NodeGraphType : InterfaceGraphType
        {
            public NodeGraphType()
            {
                Name = "Node";

                Field<NonNullGraphType<IdGraphType>>("id");
            }
        }
    }

    public static class TypeFirstSchema
    {
        public class Query
        {
            public Picture? Img => null!;
        }

        [Implements(typeof(INode))]
        [Implements(typeof(IResource))]
        [Implements(typeof(IImage))]
        public class Picture : IImage
        {
            [Id]
            public string Id => null!;
            public string? Url => null;
            public string? Thumbnail => null;
            public bool? Favorite => null;
        }

        [Implements(typeof(INode))]
        [Implements(typeof(IResource))]
        [Name("Image")]
        public interface IImage : IResource
        {
            string? Thumbnail { get; }
        }

        [Implements(typeof(INode))]
        [Name("Resource")]
        public interface IResource : INode
        {
            string? Url { get; }
        }

        [Name("Node")]
        public interface INode
        {
            [Id]
            string Id { get; }
        }
    }

    public class Interface1 : InterfaceGraphType
    {
        public Interface1()
        {
            Field<NonNullGraphType<IdGraphType>>("id");
        }
    }

    public class Interface2 : InterfaceGraphType // wider than Interface3
    {
        public Interface2()
        {
            Field<StringGraphType>("name");
        }
    }

    public class Interface3 : InterfaceGraphType // narrower than Interface2
    {
        public Interface3()
        {
            Field<NonNullGraphType<StringGraphType>>("name");
        }
    }

    public class Interface4 : InterfaceGraphType // wider than Interface5
    {
        public Interface4()
        {
            Field<StringGraphType>("name");
        }
    }

    public class Interface5 : InterfaceGraphType // narrower than Interface4
    {
        public Interface5()
        {
            Field<NonNullGraphType<StringGraphType>>("name");
            Interface<Interface4>();
        }
    }

    public class Interface6 : InterfaceGraphType // wider than Interface7
    {
        public Interface6()
        {
            Field<Interface4>("test");
        }
    }

    public class Interface7 : InterfaceGraphType // narrower than Interfac6
    {
        public Interface7()
        {
            Field<Interface5>("test");
        }
    }

    public class Interface8 : InterfaceGraphType
    {
        public Interface8()
        {
            Field<StringGraphType>("test")
                .Argument<StringGraphType>("arg");
        }
    }

    public class Interface9 : InterfaceGraphType
    {
        public Interface9()
        {
            Field<StringGraphType>("test")
                .Argument<NonNullGraphType<StringGraphType>>("arg");
        }
    }

    public class Interface10 : InterfaceGraphType
    {
        public Interface10()
        {
            Field<StringGraphType>("test");
        }
    }

    public class Interface11 : InterfaceGraphType
    {
        public Interface11()
        {
            Field<StringGraphType>("test")
                .Argument<StringGraphType>("arg")
                .Argument<StringGraphType>("arg2");
        }
    }
}
