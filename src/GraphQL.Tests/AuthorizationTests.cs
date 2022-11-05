using GraphQL.Types;

namespace GraphQL.Tests;

public class AuthorizationTests
{
    [Fact]
    public void Field()
    {
        var field = new FieldType();
        field.IsAuthorizationRequired().ShouldBeFalse();
        field.AuthorizeWithPolicy("Policy1");
        field.IsAuthorizationRequired().ShouldBeTrue();
        field.AuthorizeWithPolicy("Policy2");
        field.AuthorizeWithPolicy("Policy2");
        field.AuthorizeWithPolicy("Policy3");
        field.AuthorizeWithPolicy("Policy3");
        field.AuthorizeWithRoles("Role1,Role2");
        field.AuthorizeWithRoles("Role3, Role2");
        field.AuthorizeWithRoles("Role1", "Role4");
        field.AuthorizeWithRoles("");

        field.IsAuthorizationRequired().ShouldBeTrue();
        field.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        field.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3", "Role4" });
    }

    [Fact]
    public void NoRoles()
    {
        var field = new FieldType();
        field.AuthorizeWithRoles();
        field.AuthorizeWithRoles("");
        field.AuthorizeWithRoles(" ");
        field.AuthorizeWithRoles(",");
        field.IsAuthorizationRequired().ShouldBeTrue();
    }

    [Fact]
    public void AllowAnonymous()
    {
        var field = new FieldType();
        field.IsAnonymousAllowed().ShouldBeFalse();
        field.AllowAnonymous();
        field.IsAnonymousAllowed().ShouldBeTrue();
    }

    [Fact]
    public void Authorize()
    {
        var field = new FieldType();
        field.IsAuthorizationRequired().ShouldBeFalse();
        field.Authorize();
        field.IsAuthorizationRequired().ShouldBeTrue();
    }

    [Fact]
    public void FieldBuilder()
    {
        var graph = new ObjectGraphType();
        graph.Field<StringGraphType>("Field")
            .AuthorizeWithPolicy("Policy1")
            .AuthorizeWithPolicy("Policy2")
            .AuthorizeWithPolicy("Policy2")
            .AuthorizeWithPolicy("Policy3")
            .AuthorizeWithPolicy("Policy3")
            .AuthorizeWithRoles("Role1,Role2")
            .AuthorizeWithRoles("Role3, Role2")
            .AuthorizeWithRoles("Role1", "Role4");

        var field = graph.Fields.Find("Field");
        field.IsAuthorizationRequired().ShouldBeTrue();
        field.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        field.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3", "Role4" });
    }

    [Fact]
    public void ConnectionBuilder()
    {
        var graph = new ObjectGraphType();
        graph.Connection<StringGraphType>()
            .Name("Field")
            .AuthorizeWithPolicy("Policy1")
            .AuthorizeWithPolicy("Policy2")
            .AuthorizeWithPolicy("Policy2")
            .AuthorizeWithPolicy("Policy3")
            .AuthorizeWithPolicy("Policy3")
            .AuthorizeWithRoles("Role1,Role2")
            .AuthorizeWithRoles("Role3, Role2")
            .AuthorizeWithRoles("Role1", "Role4");

        var field = graph.Fields.Find("Field");
        field.IsAuthorizationRequired().ShouldBeTrue();
        field.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        field.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3", "Role4" });
    }

    [Fact]
    public void AutoOutputGraphType()
    {
        var graph = new AutoRegisteringObjectGraphType<Class1>();
        graph.IsAuthorizationRequired().ShouldBeTrue();
        graph.IsAnonymousAllowed().ShouldBeFalse();
        graph.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        graph.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3" });

        graph.Fields.Find("Id").IsAuthorizationRequired().ShouldBeFalse();

        var field = graph.Fields.Find("Name");
        field.IsAuthorizationRequired().ShouldBeTrue();
        field.IsAnonymousAllowed().ShouldBeFalse();
        field.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        field.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3" });

        field = graph.Fields.Find("Value");
        field.IsAuthorizationRequired().ShouldBeTrue();
        field.IsAnonymousAllowed().ShouldBeFalse();

        field = graph.Fields.Find("Public");
        field.IsAuthorizationRequired().ShouldBeFalse();
        field.IsAnonymousAllowed().ShouldBeTrue();
    }

    [Fact]
    public void SchemaBuilder()
    {
        var schema = Schema.For(
            @"
type Class1 {
  id: String!
  name: String!
  value: String!
  public: String!
}",
            configure => configure.Types.Include<Class1>());

        var graph = (ObjectGraphType)schema.AllTypes["Class1"];
        graph.IsAuthorizationRequired().ShouldBeTrue();
        graph.IsAnonymousAllowed().ShouldBeFalse();
        graph.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        graph.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3" });

        graph.Fields.Find("id").IsAuthorizationRequired().ShouldBeFalse();

        var field = graph.Fields.Find("name");
        field.IsAuthorizationRequired().ShouldBeTrue();
        field.IsAnonymousAllowed().ShouldBeFalse();
        field.GetPolicies().ShouldBe(new string[] { "Policy1", "Policy2", "Policy3" });
        field.GetRoles().ShouldBe(new string[] { "Role1", "Role2", "Role3" });

        field = graph.Fields.Find("value");
        field.IsAuthorizationRequired().ShouldBeTrue();
        field.IsAnonymousAllowed().ShouldBeFalse();

        field = graph.Fields.Find("public");
        field.IsAuthorizationRequired().ShouldBeFalse();
        field.IsAnonymousAllowed().ShouldBeTrue();
    }

    [Authorize("Policy1")]
    [Authorize("Policy2")]
    [Authorize("Policy2")]
    [Authorize(Policy = "Policy3")]
    [Authorize(Roles = "Role1,Role2")]
    [Authorize(Roles = "Role3, Role2")]
    private class Class1
    {
        public string Id { get; set; }
        [Authorize("Policy1")]
        [Authorize("Policy2")]
        [Authorize("Policy2")]
        [Authorize(Policy = "Policy3")]
        [Authorize(Roles = "Role1,Role2")]
        [Authorize(Roles = "Role3, Role2")]
        public string Name { get; set; }
        [Authorize]
        public string Value { get; set; }
        [AllowAnonymous]
        public string Public { get; set; }
    }
}
