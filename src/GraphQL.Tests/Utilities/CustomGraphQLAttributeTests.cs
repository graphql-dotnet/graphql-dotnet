using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Utilities;

public class CustomGraphQLAttributeTests : SchemaBuilderTestBase
{
    [Fact]
    public void can_set_metadata_from_custom_attribute()
    {
        var defs = @"
                type Post {
                    id: ID!
                    title: String!
                }

                type Query {
                    post(id: ID!): Post
                }
            ";

        Builder.Types.Include<PostWithExtraAttributesType>();
        Builder.Types.Include<Post>();

        var schema = Builder.Build(defs);
        schema.Initialize();

        var query = schema.AllTypes["Query"] as IObjectGraphType;
        var field = query.Fields.Single(x => x.Name == "post");
        field.GetMetadata<string>("Authorize").ShouldBe("SomePolicy");
    }

    [Fact]
    public void impl_type_sets_isTypeOfFunc()
    {
        var defs = @"
                interface IUniqueElement
                {
                    id: ID!
                }

                type ABlog implements IUniqueElement
                {
                    id: ID!
                    name: String!
                }

                type Query
                {
                    blog(id: ID!): ABlog
                }
            ";

        Builder.Types.Include<ResolvingClassForABlog>();

        var schema = Builder.Build(defs);
        schema.Initialize();

        var blog = schema.AllTypes["ABlog"] as IObjectGraphType;

        blog.IsTypeOf.ShouldNotBeNull();
        blog.IsTypeOf(new ResolvingClassForABlog()).ShouldBeTrue();
    }

    [Fact]
    public void impl_type_sets_default_isTypeOfFunc()
    {
        var defs = @"
                interface IUniqueElement
                {
                    id: ID!
                }

                type ABlog implements IUniqueElement
                {
                    id: ID!
                    name: String!
                }

                type Query
                {
                    blog(id: ID!): ABlog
                }
            ";

        Builder.Types.Include<ABlog, ABlog>();

        var schema = Builder.Build(defs);
        schema.Initialize();

        var blog = schema.AllTypes["ABlog"] as IObjectGraphType;

        blog.IsTypeOf.ShouldNotBeNull();
        blog.IsTypeOf(new ABlog()).ShouldBeTrue();
    }
}

public class MyAuthorizeAttribute : GraphQLAttribute
{
    public string Policy { get; set; }

    public override void Modify(TypeConfig type)
    {
        type.Metadata["Authorize"] = Policy;
    }

    public override void Modify(FieldConfig field)
    {
        field.Metadata["Authorize"] = Policy;
    }
}

[GraphQLMetadata("Query")]
public class PostWithExtraAttributesType
{
    [GraphQLMetadata("post"), MyAuthorize(Policy = "SomePolicy")]
    public Post GetPostById(string id)
    {
        return PostData.Posts.FirstOrDefault(x => x.Id == id);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "for tests")]
    public ResolvingClassForABlog Blog(string id)
    {
        return new ResolvingClassForABlog();
    }
}

public abstract class UniqueElement
{
    public abstract string Id { get; }
}

[GraphQLMetadata(Name = "ABlog", IsTypeOf = typeof(ResolvingClassForABlog))]
public class ResolvingClassForABlog : UniqueElement
{
    public ResolvingClassForABlog()
    {
        Id = "Id-0";
        Name = $"Blog Name {Id}";
    }

    public override string Id { get; }
    public string Name { get; }
}

public class ABlog : UniqueElement
{
    public ABlog()
    {
        Id = "Id-0";
        Name = $"Blog Name {Id}";
    }

    public override string Id { get; }
    public string Name { get; }
}
