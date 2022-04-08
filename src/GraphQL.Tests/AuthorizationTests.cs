using GraphQL.Types;

namespace GraphQL.Tests;

public class AuthorizationTests
{
    [Fact]
    public void Field()
    {
        var field = new FieldType();
        field.RequiresAuthorization().ShouldBeFalse();
        field.AuthorizeWith("Policy1");
        field.RequiresAuthorization().ShouldBeTrue();
        field.AuthorizeWith("Policy2");
        field.AuthorizeWith("Policy2");
        field.AuthorizeWithPolicy("Policy3");
        field.AuthorizeWithPolicy("Policy3");
        field.AuthorizeWithRoles("Role1,Role2");
        field.AuthorizeWithRoles("Role3, Role2");
        field.AuthorizeWithRoles("Role1", "Role4");

        field.RequiresAuthorization().ShouldBeTrue();
        field.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        field.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3", "Role4" });
    }

    [Fact]
    public void FieldBuilder()
    {
        var graph = new ObjectGraphType();
        graph.Field<StringGraphType>("Field")
            .AuthorizeWith("Policy1")
            .AuthorizeWith("Policy2")
            .AuthorizeWith("Policy2")
            .AuthorizeWithPolicy("Policy3")
            .AuthorizeWithPolicy("Policy3")
            .AuthorizeWithRoles("Role1,Role2")
            .AuthorizeWithRoles("Role3, Role2")
            .AuthorizeWithRoles("Role1", "Role4");

        var field = graph.Fields.Find("Field");
        field.RequiresAuthorization().ShouldBeTrue();
        field.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        field.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3", "Role4" });
    }

    [Fact]
    public void ConnectionBuilder()
    {
        var graph = new ObjectGraphType();
        graph.Connection<StringGraphType>()
            .Name("Field")
            .AuthorizeWith("Policy1")
            .AuthorizeWith("Policy2")
            .AuthorizeWith("Policy2")
            .AuthorizeWithPolicy("Policy3")
            .AuthorizeWithPolicy("Policy3")
            .AuthorizeWithRoles("Role1,Role2")
            .AuthorizeWithRoles("Role3, Role2")
            .AuthorizeWithRoles("Role1", "Role4");

        var field = graph.Fields.Find("Field");
        field.RequiresAuthorization().ShouldBeTrue();
        field.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        field.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3", "Role4" });
    }

    [Fact]
    public void AutoOutputGraphType()
    {
        var graph = new AutoRegisteringObjectGraphType<Class1>();
        graph.RequiresAuthorization().ShouldBeTrue();
        graph.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        graph.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3" });

        graph.Fields.Find("Id").RequiresAuthorization().ShouldBeFalse();

        var field = graph.Fields.Find("Name");
        field.RequiresAuthorization().ShouldBeTrue();
        field.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        field.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3" });
    }

    [Fact]
    public void SchemaBuilder()
    {
        var schema = Schema.For(
            @"
type Class1 {
  id: String!
  name: String!
}",
            configure => configure.Types.Include<Class1>());

        var graph = (ObjectGraphType)schema.AllTypes["Class1"];
        graph.RequiresAuthorization().ShouldBeTrue();
        graph.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        graph.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3" });

        graph.Fields.Find("id").RequiresAuthorization().ShouldBeFalse();

        var field = graph.Fields.Find("name");
        field.RequiresAuthorization().ShouldBeTrue();
        field.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        field.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3" });
    }

    [GraphQLAuthorize("Policy1")]
    [GraphQLAuthorize("Policy2")]
    [GraphQLAuthorize("Policy2")]
    [GraphQLAuthorize(Policy = "Policy3")]
    [GraphQLAuthorize(Roles = "Role1,Role2")]
    [GraphQLAuthorize(Roles = "Role3, Role2")]
    private class Class1
    {
        public string Id { get; set; }
        [GraphQLAuthorize("Policy1")]
        [GraphQLAuthorize("Policy2")]
        [GraphQLAuthorize("Policy2")]
        [GraphQLAuthorize(Policy = "Policy3")]
        [GraphQLAuthorize(Roles = "Role1,Role2")]
        [GraphQLAuthorize(Roles = "Role3, Role2")]
        public string Name { get; set; }
    }
}
