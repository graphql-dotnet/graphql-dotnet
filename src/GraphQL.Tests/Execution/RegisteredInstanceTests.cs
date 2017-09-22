using System;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class RegisteredInstanceTests : BasicQueryTestBase
    {
        [Fact]
        public void build_dynamic_schema()
        {
            var schema = new Schema();

            var person = new ObjectGraphType();
            person.Name = "Person";
            person.Field("name", new StringGraphType());
            person.Field(
                "friends",
                new ListGraphType(new NonNullGraphType(person)),
                resolve: ctx => new[] {new SomeObject {Name = "Jaime"}, new SomeObject {Name = "Joe"}});

            var root = new ObjectGraphType();
            root.Name = "Root";
            root.Field("hero", person, resolve: ctx => ctx.RootValue);

            schema.Query = root;
            schema.RegisterTypes(person);

            AssertQuerySuccess(
                schema,
                @"{ hero { name friends { name } } }",
                @"{ hero: { name : 'Quinn', friends: [ { name: 'Jaime' }, { name: 'Joe' }] } }",
                root: new SomeObject { Name = "Quinn"});
        }

        [Fact]
        public void build_union()
        {
            var schema = new Schema();

            var person = new ObjectGraphType();
            person.Name = "Person";
            person.Field("name", new StringGraphType());
            person.IsTypeOf = type => true;

            var robot = new ObjectGraphType();
            robot.Name = "Robot";
            robot.Field("name", new StringGraphType());
            robot.IsTypeOf = type => true;

            var personOrRobot = new UnionGraphType();
            personOrRobot.Name = "PersonOrRobot";
            personOrRobot.AddPossibleType(person);
            personOrRobot.AddPossibleType(robot);

            var root = new ObjectGraphType();
            root.Name = "Root";
            root.Field("hero", personOrRobot, resolve: ctx => ctx.RootValue);

            schema.Query = root;

            AssertQuerySuccess(
                schema,
                @"{ hero {
                    ... on Person { name }
                    ... on Robot { name }
                } }",
                @"{ hero: { name : 'Quinn' }}",
                root: new SomeObject { Name = "Quinn"});
        }

        [Fact]
        public void build_nested_type_with_list()
        {
            build_schema("list").ShouldBeCrossPlat(@"schema {
  query: root
}

# The `Date` scalar type represents a timestamp provided in UTC. `Date` expects
# timestamps to be formatted in accordance with the
# [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.
scalar Date

scalar Decimal

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

# The `Date` scalar type represents a timestamp provided in UTC. `Date` expects
# timestamps to be formatted in accordance with the
# [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.
scalar Date

scalar Decimal

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

# The `Date` scalar type represents a timestamp provided in UTC. `Date` expects
# timestamps to be formatted in accordance with the
# [ISO-8601](https://en.wikipedia.org/wiki/ISO_8601) standard.
scalar Date

scalar Decimal

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
            var nestedObjType = new ObjectGraphType()
            {
                Name = "NestedObjType"
            };
            nestedObjType.AddField(new FieldType()
            {
                ResolvedType = new IntGraphType(),
                Name = "intField"
            });
            var rootType = new ObjectGraphType {Name = "root"};
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

            rootType.AddField(new FieldType()
            {
                Name = "listOfObjField",
                ResolvedType = resolvedType
            });

            var s = new Schema()
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

    public static class ObjectGraphTypeExtensions
    {
        public static void Field(
            this IObjectGraphType obj,
            string name,
            IGraphType type,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
        {
            var field = new FieldType();
            field.Name = name;
            field.Description = description;
            field.Arguments = arguments;
            field.ResolvedType = type;
            field.Resolver = resolve != null ? new FuncFieldResolver<object>(resolve) : null;
            obj.AddField(field);
        }
    }

    public static class AssertionExtensions
    {
        public static void ShouldBeCrossPlat(this string a, string b)
        {
            var aa = a?.Replace("\r\n", "\n");
            var bb = b?.Replace("\r\n", "\n");
            aa.ShouldBe(bb);
        }
    }
}
