using System;
using System.Linq;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Utilities.Federation;
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
        public void registers_argument_input_objects_when_argument_resolved_type_is_set()
        {
            var schema = new ARootSchema();
            schema.FindType("a");

            ContainsTypeNames(
                schema,
                "DInputType",
                "DInputType2");
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
            //note: The exception occurs during Schema.CreateTypesLookup(), not during Schema.FindType()
            Should.Throw<InvalidOperationException>(() => schema.FindType("a"));
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
        public void registers_additional_duplicated_types()
        {
            var schema = new SchemaWithDuplicates();
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

        [Fact]
        public void handles_cycle_field_type()
        {
            var schema = new SimpleCycleSchema();
            schema.FindType("CycleType").ShouldNotBeNull();
        }

        [Fact]
        public void handles_stackoverflow_exception_for_cycle_field_type()
        {
            var schema = new ACyclingDerivingSchema(new FuncServiceProvider(t => t == typeof(AbstractGraphType) ? new ConcreteGraphType() : null));
            Should.Throw<InvalidOperationException>(() => schema.FindType("abcd"));
        }

        private void ContainsTypeNames(ISchema schema, params string[] typeNames)
        {
            typeNames.Apply(typeName =>
            {
                var type = schema.FindType(typeName);
                type.ShouldNotBeNull($"Did not find {typeName} in type lookup.");
            });
        }

        private void DoesNotContainTypeNames(Schema schema, params string[] typeNames)
        {
            typeNames.Apply(typeName =>
            {
                var type = schema.AllTypes.SingleOrDefault(x => x.Name == typeName);
                type.ShouldBe(null, $"Found {typeName} in type lookup.");
            });
        }

        [Fact]
        public void middleware_can_reference_SchemaTypes()
        {
            var schema = new Schema { Query = new SomeQuery() };
            schema.FieldMiddleware.Use(next =>
            {
                schema.AllTypes.Count.ShouldNotBe(0);
                return async context =>
                {
                    var res = await next(context);
                    return "One " + res.ToString();
                };
            });
            schema.Initialize();
        }

        [Fact]
        public void disposed_schema_throws_errors()
        {
            var schema = new Schema();
            schema.Dispose();
            Should.Throw<ObjectDisposedException>(() => schema.Initialize());
            Should.Throw<ObjectDisposedException>(() => schema.RegisterType(new ObjectGraphType { Name = "test" }));
            Should.Throw<ObjectDisposedException>(() => schema.RegisterTypes(new IGraphType[] { }));
            Should.Throw<ObjectDisposedException>(() => schema.RegisterTypes(typeof(DroidType)));
            Should.Throw<ObjectDisposedException>(() => schema.RegisterType<DroidType>());
            Should.Throw<ObjectDisposedException>(() => schema.RegisterDirective(new DirectiveGraphType("test", new DirectiveLocation[] { DirectiveLocation.Field })));
            Should.Throw<ObjectDisposedException>(() => schema.RegisterDirectives(new DirectiveGraphType[] { }));
            Should.Throw<ObjectDisposedException>(() => schema.RegisterValueConverter(new AnyValueConverter()));
        }

        [Fact]
        public void initialized_schema_throws_errors()
        {
            var schema = new Schema();
            schema.Initialize();
            Should.Throw<InvalidOperationException>(() => schema.RegisterType(new ObjectGraphType { Name = "test" }));
            Should.Throw<InvalidOperationException>(() => schema.RegisterTypes(new IGraphType[] { }));
            Should.Throw<InvalidOperationException>(() => schema.RegisterTypes(typeof(DroidType)));
            Should.Throw<InvalidOperationException>(() => schema.RegisterType<DroidType>());
            Should.Throw<InvalidOperationException>(() => schema.RegisterDirective(new DirectiveGraphType("test", new DirectiveLocation[] { DirectiveLocation.Field })));
            Should.Throw<InvalidOperationException>(() => schema.RegisterDirectives(new DirectiveGraphType[] { }));
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

    public class SchemaWithDuplicates : Schema
    {
        public SchemaWithDuplicates()
        {
            Query = new SomeQuery();

            RegisterType<SomeObject>();
            RegisterType<SomeObject>();
            RegisterType<SomeQuery>();
            RegisterType<SomeQuery>();
            RegisterType<SomeInterface>();
            RegisterType<SomeInterface>();
            RegisterType<StringGraphType>();
            RegisterType<StringGraphType>();
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
            Field<StringGraphType>("id", resolve: ctx => new { id = "id" });
            Field<StringGraphType>(
                "filter",
                arguments: new QueryArguments(new QueryArgument<DInputType> { Name = "input", ResolvedType = new DInputType() }, new QueryArgument<DInputType2> { Name = "input2", ResolvedType = new DInputType2() }),
                resolve: ctx => new { id = "id" });
            Field<ListGraphType<DListType>>("alist");
        }
    }

    public class DInputType : InputObjectGraphType
    {
        public DInputType()
        {
            Name = "DInputType";
            Field<StringGraphType>("one");
        }
    }

    public class DInputType2 : InputObjectGraphType
    {
        public DInputType2()
        {
            Name = "DInputType2";
            Field<StringGraphType>("two");
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
    }

    public class WithoutIsTypeOf2Type : ObjectGraphType
    {
    }

    public class SimpleCycleSchema : Schema
    {
        public SimpleCycleSchema()
        {
            Query = new CycleType();
        }
    }

    public class CycleType : ObjectGraphType
    {
        public CycleType()
        {
            Field<CycleType>();
        }
    }

    public class ACyclingDerivingSchema : Schema
    {
        public ACyclingDerivingSchema(IServiceProvider provider) : base(provider)
        {
            Query = new CyclingQueryType();
        }
    }

    public class CyclingQueryType : ObjectGraphType
    {
        public CyclingQueryType()
        {
            Field<AbstractGraphType>();
        }
    }

    public abstract class AbstractGraphType : ObjectGraphType
    {
        public AbstractGraphType()
        {
            Field<AbstractGraphType>();
        }
    }

    public class ConcreteGraphType : AbstractGraphType
    {
    }
}
