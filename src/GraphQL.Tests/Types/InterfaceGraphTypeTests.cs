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
        ValidateSchema(schema);
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
        ValidateSchema(schema);
    }

    [Fact]
    public void SupportsInterfaceInheritance_CodeFirst2()
    {
        var schema = new MySchema2();
        schema.Initialize();
        ValidateSchema(schema);
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
        ValidateSchema(schema);
    }

    private void ValidateSchema(ISchema schema)
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

    public class TypeFirstSchema
    {
        public class Query
        {
            public Picture? Img => null!;
        }

        [Implements(typeof(Node))]
        [Implements(typeof(Resource))]
        [Implements(typeof(Image))]
        public class Picture
        {
            [Id]
            public string Id => null!;
            public string? Url => null;
            public string? Thumbnail => null;
            public bool? Favorite => null;
        }

        [Implements(typeof(Node))]
        [Implements(typeof(Resource))]
        public interface Image
        {
            [Id]
            string Id { get; }
            string? Url { get; }
            string? Thumbnail { get; }
        }

        [Implements(typeof(Node))]
        public interface Resource
        {
            [Id]
            string Id { get; }
            string? Url { get; }
        }

        public interface Node
        {
            [Id]
            string Id { get; }
        }
    }
}
