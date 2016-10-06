using System;
using System.Linq;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Types
{
    public class SchemaTests
    {
        [Fact]
        public void registers_interfaces_when_not_used_in_fields()
        {
            var schema = new AnInterfaceSchema();
            schema.FindType("a");
            var result = schema.AllTypes.SingleOrDefault(x => x.Name == "AnInterfaceType");
            result.ShouldNotBeNull("Interface type should be registered");
        }

        [Fact]
        public void recursively_registers_children()
        {
            var schema = new ARootSchema();
            schema.FindType("a");

            ContainsTypeNames(
                schema,
                "RootSchemaType",
                "ASchemaType",
                "BSchemaType",
                "CSchemaType",
                "DSchemaType");
        }

        [Fact]
        public void registers_argument_input_objects()
        {
            var schema = new ARootSchema();
            schema.FindType("a");

            ContainsTypeNames(
                schema,
                "DInputType");
        }

        [Fact]
        public void registers_type_when_list()
        {
            var schema = new ARootSchema();
            schema.FindType("a");

            ContainsTypeNames(
                schema,
                "DListType");
        }

        [Fact]
        public void registers_union_types()
        {
            var schema = new ARootSchema();
            schema.FindType("a");

            ContainsTypeNames(
                schema,
                "AUnion",
                "WithoutIsTypeOf1Type",
                "WithoutIsTypeOf2Type");
        }

        [Fact]
        public void throw_error_on_missing_istypeof()
        {
            var schema = new InvalidUnionSchema();
            Should.Throw<ExecutionError>(() => schema.FindType("a"));
        }

        [Fact]
        public void throw_error_on_non_graphtype_with_register_types()
        {
            var schema = new Schema();
            Should.Throw<ArgumentOutOfRangeException>(() => schema.RegisterTypes(typeof(MyDto)));
        }

        [Fact]
        public void throw_error_on_null_with_register_types()
        {
            var schema = new Schema();
            Type[] types = null;
            Should.Throw<ArgumentNullException>(() => schema.RegisterTypes(types));
        }

        [Fact]
        public void registers_additional_types()
        {
            var schema = new AnInterfaceOnlySchemaWithExtraRegisteredType();
            schema.FindType("abcd");

            ContainsTypeNames(schema, "SomeQuery", "SomeInterface", "SomeObject");
        }

        [Fact]
        public void registers_only_root_types()
        {
            var schema = new ARootSchema();
            schema.FindType("abcd");

            DoesNotContainTypeNames(schema, "ASchemaType!");
        }

        public void ContainsTypeNames(ISchema schema, params string[] typeNames)
        {
            typeNames.Apply(typeName =>
            {
                var type = schema.FindType(typeName);
                type.ShouldNotBeNull("Did not find {0} in type lookup.".ToFormat(typeName));
            });
        }

        public void DoesNotContainTypeNames(Schema schema, params string[] typeNames)
        {
            typeNames.Apply(typeName =>
            {
                var type = schema.AllTypes.SingleOrDefault(x => x.Name == typeName);
                type.ShouldBe(null, "Found {0} in type lookup.".ToFormat(typeName));
            });
        }
    }

    public class MyDto
    {
    }

    public class AnInterfaceOnlySchemaWithExtraRegisteredType : Schema
    {
        public AnInterfaceOnlySchemaWithExtraRegisteredType()
        {
            Query = new SomeQuery();

            RegisterType<SomeObject>();
        }
    }

    public class SomeQuery : ObjectGraphType
    {
        public SomeQuery()
        {
            Name = "SomeQuery";
            Field<SomeInterface>("something");
        }
    }

    public class SomeObject : ObjectGraphType
    {
        public SomeObject()
        {
            Name = "SomeObject";
            Field<StringGraphType>("name");
            Interface<SomeInterface>();

            IsTypeOf = t => true;
        }
    }

    public class SomeInterface : InterfaceGraphType
    {
        public SomeInterface()
        {
            Name = "SomeInterface";
            Field<StringGraphType>("name");
        }
    }

    public class AnInterfaceSchema : Schema
    {
        public AnInterfaceSchema()
        {
            Query = new AnObjectType();
        }
    }

    public class AnObjectType : ObjectGraphType
    {
        public AnObjectType()
        {
            Name = "AnObjectType";
            Field<StringGraphType>("name");
            Interface<AnInterfaceType>();
        }
    }

    public class AnInterfaceType : InterfaceGraphType
    {
        public AnInterfaceType()
        {
            Name = "AnInterfaceType";
            Field<StringGraphType>("name");
            ResolveType = value => null;
        }
    }

    public class ARootSchema : Schema
    {
        public ARootSchema()
        {
            Query = new RootSchemaType();
        }
    }

    public class RootSchemaType : ObjectGraphType
    {
        public RootSchemaType()
        {
            Field<ASchemaType>("a");
            Field<NonNullGraphType<ASchemaType>>("nonNullA");
            Field<AUnionType>("union");
        }
    }

    public class InvalidUnionSchema : Schema
    {
        public InvalidUnionSchema()
        {
            Query = new InvalidUnionSchemaType();
        }
    }

    public class InvalidUnionSchemaType : ObjectGraphType
    {
        public InvalidUnionSchemaType()
        {
            Field<AUnionWithoutResolveType>("union");
        }
    }

    public class ASchemaType : ObjectGraphType
    {
        public ASchemaType()
        {
            Field<BSchemaType>("b");
        }
    }

    public class BSchemaType : ObjectGraphType
    {
        public BSchemaType()
        {
            Field<CSchemaType>("c");
        }
    }

    public class CSchemaType : ObjectGraphType
    {
        public CSchemaType()
        {
            Field<DSchemaType>("d");
        }
    }

    public class DSchemaType : ObjectGraphType
    {
        public DSchemaType()
        {
            Field<StringGraphType>("id", resolve: ctx => new {id = "id"});
            Field<StringGraphType>(
                "filter",
                arguments: new QueryArguments(new [] { new QueryArgument<DInputType> {Name = "input"} }),
                resolve: ctx => new {id = "id"});
            Field<ListGraphType<DListType>>("alist");
        }
    }

    public class DInputType : InputObjectGraphType
    {
        public DInputType()
        {
            Field<StringGraphType>("one");
        }
    }

    public class DListType : ObjectGraphType
    {
        public DListType()
        {
            Field<StringGraphType>("list");
        }
    }

    public class AUnionType : UnionGraphType
    {
        public AUnionType()
        {
            Name = "AUnion";
            Type<WithoutIsTypeOf1Type>();
            Type<WithoutIsTypeOf2Type>();
            ResolveType = value => null;
        }
    }

    public class AUnionWithoutResolveType : UnionGraphType
    {
        public AUnionWithoutResolveType()
        {
            Name = "AUnionWithoutResolve";
            Type<WithoutIsTypeOf1Type>();
            Type<WithoutIsTypeOf2Type>();
        }
    }

    public class WithoutIsTypeOf1Type : ObjectGraphType
    {
        public WithoutIsTypeOf1Type()
        {
        }
    }

    public class WithoutIsTypeOf2Type : ObjectGraphType
    {
        public WithoutIsTypeOf2Type()
        {
        }
    }
}
