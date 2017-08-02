using System;
using System.Linq;
using System.Reflection;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Utilities
{
    public class CustomSchemaBuilderTests : SchemaBuilderTestBase
    {
        public CustomSchemaBuilderTests()
        {
            Builder = new MyCustomSchemaBuilder();
        }

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

            var schema = Builder.Build(defs);
            schema.Initialize();

            var query = schema.FindType("Query") as IObjectGraphType;
            var field = query.Fields.Single(x => x.Name == "post");
            field.GetMetadata<string>("Authorize").ShouldBe("SomePolicy");
        }
    }

    public class MyAuthorizeAttribute : Attribute
    {
        public string Policy { get; set; }
    }

    public class MyCustomSchemaBuilder : SchemaBuilder
    {
        protected override FieldType ToFieldType(string parentTypeName, GraphQLFieldDefinition fieldDef)
        {
            var typeConfig = Types.For(parentTypeName);

            var fieldType = base.ToFieldType(parentTypeName, fieldDef);

            var methodInfo = typeConfig.MethodForField(fieldType.Name);

            var attr = methodInfo?.GetCustomAttribute<MyAuthorizeAttribute>();
            if (attr != null)
            {
                fieldType.Metadata["Authorize"] = attr.Policy;
            }

            return fieldType;
        }
    }

    [GraphQLName("Query")]
    public class PostWithExtraAttributesType
    {
        [GraphQLName("post")]
        [MyAuthorize(Policy = "SomePolicy")]
        public Post GetPostById(string id)
        {
            return PostData.Posts.FirstOrDefault(x => x.Id == id);
        }
    }
}
