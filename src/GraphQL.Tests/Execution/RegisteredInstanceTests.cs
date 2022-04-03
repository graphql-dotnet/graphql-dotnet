using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Execution;

public class RegisteredInstanceTests : BasicQueryTestBase
{
    public class Product
    {
        public string Name { get; set; }
    }

    [Fact]
    public void nested_groups_work()
    {
        var product = new ObjectGraphType { Name = "Product" };
        product.Field("name", new StringGraphType());
        product.IsTypeOf = obj => obj is Product;

        var catalog = new ObjectGraphType { Name = "Catalog" };
        catalog.Field("products", new ListGraphType(product), resolve: ctx =>
        {
            return new List<Product> {
                new Product { Name = "Book" }
            };
        });

        var retail = new ObjectGraphType { Name = "Retail" };
        retail.Field("catalog", catalog, resolve: ctx => new { });

        var root = new ObjectGraphType { Name = "Root" };
        root.Field("retail", retail, resolve: ctx => new { });

        var schema = new Schema { Query = root };
        schema.RegisterTypes(retail);
        schema.RegisterTypes(catalog);

        AssertQuerySuccess(
            schema,
            @"{ retail { catalog { products { name } } } }",
            @"{ ""retail"": { ""catalog"": { ""products"": [ { ""name"": ""Book"" }] } } }"
        );
    }

    [Fact]
    public void build_dynamic_schema()
    {
        var schema = new Schema();

        var person = new ObjectGraphType { Name = "Person" };
        person.Field("name", new StringGraphType());
        person.Field(
            "friends",
            new ListGraphType(new NonNullGraphType(person)),
            resolve: ctx => new[] { new SomeObject { Name = "Jaime" }, new SomeObject { Name = "Joe" } });

        var root = new ObjectGraphType { Name = "Root" };
        root.Field("hero", person, resolve: ctx => ctx.RootValue);

        schema.Query = root;
        schema.RegisterTypes(person);

        AssertQuerySuccess(
            schema,
            @"{ hero { name friends { name } } }",
            @"{ ""hero"": { ""name"": ""Quinn"", ""friends"": [ { ""name"": ""Jaime"" }, { ""name"": ""Joe"" }] } }",
            root: new SomeObject { Name = "Quinn" });
    }

    [Fact]
    public void build_union()
    {
        var schema = new Schema();

        var person = new ObjectGraphType { Name = "Person" };
        person.Field("name", new StringGraphType());
        person.IsTypeOf = type => true;

        var robot = new ObjectGraphType { Name = "Robot" };
        robot.Field("name", new StringGraphType());
        robot.IsTypeOf = type => true;

        var personOrRobot = new UnionGraphType { Name = "PersonOrRobot" };
        personOrRobot.AddPossibleType(person);
        personOrRobot.AddPossibleType(robot);

        var root = new ObjectGraphType { Name = "Root" };
        root.Field("hero", personOrRobot, resolve: ctx => ctx.RootValue);

        schema.Query = root;

        AssertQuerySuccess(
            schema,
            @"{ hero {
                    ... on Person { name }
                    ... on Robot { name }
                } }",
            @"{ ""hero"": { ""name"" : ""Quinn"" }}",
            root: new SomeObject { Name = "Quinn" });
    }

    [Fact]
    public void build_nested_type_with_list()
    {
        build_schema("list").ShouldBeCrossPlat(@"schema {
  query: root
}

type NestedObjType {
  intField: Int
}

type root {
  listOfObjField: [NestedObjType]
}
");
    }

    [Fact]
    public void build_nested_type_with_non_null()
    {
        build_schema("non-null").ShouldBeCrossPlat(@"schema {
  query: root
}

type NestedObjType {
  intField: Int
}

type root {
  listOfObjField: NestedObjType!
}
");
    }

    [Fact]
    public void build_nested_type_with_base()
    {
        build_schema("none").ShouldBeCrossPlat(@"schema {
  query: root
}

type NestedObjType {
  intField: Int
}

type root {
  listOfObjField: NestedObjType
}
");
    }

    private string build_schema(string propType)
    {
        var nestedObjType = new ObjectGraphType
        {
            Name = "NestedObjType"
        };
        nestedObjType.AddField(new FieldType
        {
            ResolvedType = new IntGraphType(),
            Name = "intField"
        });
        var rootType = new ObjectGraphType { Name = "root" };
        IGraphType resolvedType;
        switch (propType)
        {
            case "none":
            {
                resolvedType = nestedObjType;
                break;
            }
            case "list":
            {
                resolvedType = new ListGraphType(nestedObjType);
                break;
            }
            case "non-null":
            {
                resolvedType = new NonNullGraphType(nestedObjType);
                break;
            }
            default:
            {
                throw new NotSupportedException();
            }
        }

        rootType.AddField(new FieldType
        {
            Name = "listOfObjField",
            ResolvedType = resolvedType
        });

        var s = new Schema
        {
            Query = rootType
        };
        var schema = new SchemaPrinter(s).Print();
        return schema;
    }

    public class SomeObject
    {
        public string Name { get; set; }
    }
}
