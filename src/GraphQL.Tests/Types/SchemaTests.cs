using GraphQL.StarWars.Types;
using GraphQL.Types;
using GraphQL.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Types;

public class SchemaTests
{
    [Fact]
    public void registers_interfaces_when_not_used_in_fields()
    {
        var schema = new AnInterfaceSchema();
        var result = schema.AllTypes.SingleOrDefault(x => x.Name == "AnInterfaceType");
        result.ShouldNotBeNull("Interface type should be registered");
    }

    [Fact]
    public void recursively_registers_children()
    {
        var schema = new ARootSchema();

        ContainsTypeNames(
            schema,
            "RootSchema",
            "ASchema",
            "BSchema",
            "CSchema",
            "DSchema");
    }

    [Fact]
    public void registers_argument_input_objects()
    {
        var schema = new ARootSchema();

        ContainsTypeNames(
            schema,
            "DInputType");
    }

    [Fact]
    public void registers_argument_input_objects_when_argument_resolved_type_is_set()
    {
        var schema = new ARootSchema();

        ContainsTypeNames(
            schema,
            "DInputType",
            "DInputType2");
    }

    [Fact]
    public void registers_type_when_list()
    {
        var schema = new ARootSchema();

        ContainsTypeNames(
            schema,
            "DList");
    }

    [Fact]
    public void registers_union_types()
    {
        var schema = new ARootSchema();

        ContainsTypeNames(
            schema,
            "AUnion",
            "WithoutIsTypeOf1",
            "WithoutIsTypeOf2");
    }

    [Fact]
    public void throw_error_on_missing_istypeof()
    {
        var schema = new InvalidUnionSchema();
        Should.Throw<InvalidOperationException>(() => schema.AllTypes["a"]);
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
        Type[]? types = null;
        Should.Throw<ArgumentNullException>(() => schema.RegisterTypes(types!));
    }

    [Fact]
    public void registers_additional_types()
    {
        var schema = new AnInterfaceOnlySchemaWithExtraRegisteredType();

        ContainsTypeNames(schema, "SomeQuery", "SomeInterface", "SomeObject");
    }

    [Fact]
    public void registers_additional_duplicated_types()
    {
        var schema = new SchemaWithDuplicates();

        ContainsTypeNames(schema, "SomeQuery", "SomeInterface", "SomeObject");
    }

    [Fact]
    public void registers_only_root_types()
    {
        var schema = new ARootSchema();

        DoesNotContainTypeNames(schema, "ASchema!");
    }

    [Fact]
    public void handles_cycle_field_type()
    {
        var schema = new SimpleCycleSchema();
        schema.AllTypes["Cycle"].ShouldNotBeNull();
    }

    [Fact]
    public void handles_stackoverflow_exception_for_cycle_field_type()
    {
        var schema = new ACyclingDerivingSchema(new FuncServiceProvider(t => t == typeof(AbstractGraphType) ? new ConcreteGraphType() : null));
        Should.Throw<InvalidOperationException>(() => schema.AllTypes["abcd"]);
    }

    private void ContainsTypeNames(ISchema schema, params string[] typeNames)
    {
        foreach (string typeName in typeNames)
        {
            var type = schema.AllTypes[typeName];
            type.ShouldNotBeNull($"Did not find {typeName} in type lookup.");
        }
    }

    private void DoesNotContainTypeNames(Schema schema, params string[] typeNames)
    {
        foreach (string typeName in typeNames)
        {
            var type = schema.AllTypes.SingleOrDefault(x => x.Name == typeName);
            type.ShouldBe(null, $"Found {typeName} in type lookup.");
        }
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
                object? res = await next(context);
                return "One " + res;
            };
        });
        schema.Initialize();
    }

    [Fact]
    public void disposed_schema_throws_errors()
    {
        var schema = new Schema();

        schema.Initialized.ShouldBeFalse();
        schema.Dispose();
        schema.Dispose();
        schema.Initialized.ShouldBeFalse();

        Should.Throw<ObjectDisposedException>(() => schema.Initialize());
        Should.Throw<ObjectDisposedException>(() => schema.RegisterType(new ObjectGraphType { Name = "test" }));
        Should.Throw<ObjectDisposedException>(() => schema.RegisterTypes(typeof(DroidType)));
        Should.Throw<ObjectDisposedException>(() => schema.RegisterType<DroidType>());
    }

    [Fact]
    public void initialized_schema_should_throw_error_when_register_type_or_directive()
    {
        var schema = new Schema()
        {
            Query = new DummyType()
        };

        schema.Initialized.ShouldBeFalse();
        schema.Initialize();
        schema.Initialized.ShouldBeTrue();

        Should.Throw<InvalidOperationException>(() => schema.RegisterType(new ObjectGraphType { Name = "test" }));
        Should.Throw<InvalidOperationException>(() => schema.RegisterTypes(typeof(DroidType)));
        Should.Throw<InvalidOperationException>(() => schema.RegisterType<DroidType>());
    }

    [Fact]
    public void generic_types_of_mapped_clr_reference_types_should_resolve()
    {
        var schema = new Schema();
        var query = new ObjectGraphType();
        var field = query.Field("test", typeof(ConnectionType<GraphQLClrOutputTypeReference<MyDto>>));
        schema.Query = query;
        schema.RegisterTypeMapping<MyDto, MyDtoGraphType>();
        schema.Initialize();
        field.FieldType.ResolvedType.ShouldNotBeNull();
        field.FieldType.ResolvedType.ShouldBeOfType<ConnectionType<MyDtoGraphType>>();
    }

    [Fact]
    public void can_have_unknown_input_types_mapped_to_auto_registering_graph()
    {
        var schema = new CustomTypesSchema();
        var query = new ObjectGraphType();
        var field = new FieldType()
        {
            Name = "test",
            Type = typeof(IntGraphType),
            Arguments =
            [
                new QueryArgument(typeof(GraphQLClrInputTypeReference<CustomData>)) { Name = "arg" }
            ]
        };
        query.AddField(field);
        schema.Query = query;
        schema.Initialize();
        schema.Query.Fields.Find("test")!.Arguments![0].ResolvedType.ShouldBeOfType<AutoRegisteringInputObjectGraphType<CustomData>>();
    }

    [Fact]
    public void OnBeforeInitializeType_cannot_access_AllTypes()
    {
        var schema = new SchemaWithOnBeforeInitializeTypeAccessingAllTypes();

        // Attempting to initialize should throw an exception when OnBeforeInitializeType tries to access AllTypes
        Should.Throw<InvalidOperationException>(() => schema.Initialize())
            .Message.ShouldBe("Cannot access AllTypes while schema types are being created. AllTypes is not available during OnBeforeInitializeType execution.");
    }

    [Fact]
    public void does_not_double_initialize_types()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddSchema<AutoMapSchema>()
            .AddAutoClrMappings(false, false)
            .AddSystemTextJson()
            .AddResolveFieldContextAccessor()
        );
        services.AddTransient(typeof(AutoRegisteringObjectGraphType<>), typeof(MyAuto<>));
        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        var parent = schema.Query;
        var field1 = parent.Fields.Find("field1").ShouldNotBeNull();
        var field2 = parent.Fields.Find("field2").ShouldNotBeNull();
        var objType = schema.AllTypes["Obj"];
        field1.ResolvedType.ShouldBeSameAs(objType);
        field2.ResolvedType.ShouldBeSameAs(objType);
    }

    private class MyAuto<T> : AutoRegisteringObjectGraphType<T>;
    private class AutoMapSchema : Schema
    {
        public AutoMapSchema(IServiceProvider provider) : base(provider)
        {
            var query = new ObjectGraphType
            {
                Name = "Query",
            };
            query.Field<ObjType>("field1", true).Resolve(_ => new ObjType());
            query.Field<ObjType>("field2", true).Resolve(_ => new ObjType());
            Query = query;
        }

        [MapAutoClrType]
        [Name("Obj")]
        public class ObjType
        {
            public static string Hello => "world";
        }
    }
}

public class SchemaWithOnBeforeInitializeTypeAccessingAllTypes : Schema
{
    public SchemaWithOnBeforeInitializeTypeAccessingAllTypes()
    {
        var query = new ObjectGraphType { Name = "Query" };
        query.Field<StringGraphType>("test");
        Query = query;
    }

    protected override void OnBeforeInitializeType(IGraphType graphType)
    {
        // This should throw an exception because AllTypes is not available during initialization
        _ = AllTypes.Count;
    }
}

public class CustomData
{
    public string Value { get; set; }
}

public class CustomTypesSchema : Schema
{
    protected override SchemaTypes CreateSchemaTypes()
        => new CustomSchemaTypes(this, this);
}

public class CustomSchemaTypes : LegacySchemaTypes
{
    public CustomSchemaTypes(ISchema schema, IServiceProvider serviceProvider)
        : base(schema, serviceProvider)
    {
    }

    protected override Type? GetGraphTypeFromClrType(Type clrType, bool isInputType, IEnumerable<IGraphTypeMappingProvider>? typeMappings)
    {
        var ret = base.GetGraphTypeFromClrType(clrType, isInputType, typeMappings);

        if (ret == null && isInputType)
        {
            return typeof(AutoRegisteringInputObjectGraphType<>).MakeGenericType(clrType);
        }

        return ret;
    }
}

public class MyDtoGraphType : ObjectGraphType<MyDto>
{
    public MyDtoGraphType()
    {
        Field<BooleanGraphType>("dummy");
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

        this.RegisterType<SomeObject>();
    }
}

public class SchemaWithDuplicates : Schema
{
    public SchemaWithDuplicates()
    {
        Query = new SomeQuery();

        this.RegisterType<SomeObject>();
        this.RegisterType<SomeObject>();
        this.RegisterType<SomeQuery>();
        this.RegisterType<SomeQuery>();
        this.RegisterType<SomeInterface>();
        this.RegisterType<SomeInterface>();
        this.RegisterType<StringGraphType>();
        this.RegisterType<StringGraphType>();
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
        Field<StringGraphType>("id").Resolve(_ => new { id = "id" });
        Field<StringGraphType>("filter")
            .Arguments(new QueryArgument<DInputType> { Name = "input", ResolvedType = new DInputType() }, new QueryArgument<DInputType2> { Name = "input2", ResolvedType = new DInputType2() })
            .Resolve(_ => new { id = "id" });
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
    public WithoutIsTypeOf1Type()
    {
        Field<StringGraphType>("unused");
    }
}

public class WithoutIsTypeOf2Type : ObjectGraphType
{
    public WithoutIsTypeOf2Type()
    {
        Field<StringGraphType>("unused");
    }
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
        Field<CycleType>("_");
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
        Field<AbstractGraphType>("_");
    }
}

public abstract class AbstractGraphType : ObjectGraphType
{
    public AbstractGraphType()
    {
        Field<AbstractGraphType>("_");
    }
}

public class ConcreteGraphType : AbstractGraphType
{
}
