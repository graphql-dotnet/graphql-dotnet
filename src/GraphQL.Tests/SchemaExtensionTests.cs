using System.Reflection;
using GraphQL.Types;
using Moq;

namespace GraphQL.Tests;

public class SchemaExtensionTests
{
    [Fact]
    public void RegisterTypeMappings()
    {
        var actual = new List<(Type ClrType, Type GraphType)>();
        var mockSchema = new Mock<ISchema>(MockBehavior.Strict);
        mockSchema.Setup(x => x.RegisterTypeMapping(It.IsAny<Type>(), It.IsAny<Type>()))
            .Callback<Type, Type>((clrType, graphType) => actual.Add((clrType, graphType))).Verifiable();
        var schema = mockSchema.Object;
        schema.RegisterTypeMappings();
        var expected = Assembly.GetExecutingAssembly().GetClrTypeMappings();
        expected.Count.ShouldBeGreaterThan(0);
        actual.ShouldBe(expected);
        mockSchema.Verify();
    }

    [Fact]
    public void RegisterTypeMappings_Assembly()
    {
        var assembly = typeof(GraphQL.StarWars.StarWarsQuery).Assembly;
        var actual = new List<(Type ClrType, Type GraphType)>();
        var mockSchema = new Mock<ISchema>(MockBehavior.Strict);
        mockSchema.Setup(x => x.RegisterTypeMapping(It.IsAny<Type>(), It.IsAny<Type>()))
            .Callback<Type, Type>((clrType, graphType) => actual.Add((clrType, graphType))).Verifiable();
        var schema = mockSchema.Object;
        schema.RegisterTypeMappings(assembly);
        var expected = assembly.GetClrTypeMappings();
        expected.Count.ShouldBeGreaterThan(0);
        actual.ShouldBe(expected);
        mockSchema.Verify();
    }

    [Fact]
    public void RegisterTypeMappings_Null()
    {
        Should.Throw<ArgumentNullException>(() => new Schema().RegisterTypeMappings(null!));
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/3507
    [Fact]
    public void RegisterTypeMapping_With_GraphType_Instead_Of_ClrType_Should_Throw()
    {
        var ex = Should.Throw<ArgumentException>(() => new Schema().RegisterTypeMapping<IntGraphType, IntGraphType>());
        ex.ParamName.ShouldBe("clrType");
        ex.Message.ShouldStartWith("GraphQL.Types.IntGraphType' is already a GraphType (i.e. not CLR type like System.DateTime or System.String). You must specify CLR type instead of GraphType.");
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/3507
    [Fact]
    public void RegisterTypeMapping_With_ClrType_Instead_Of_GraphType_Should_Throw()
    {
        var ex = Should.Throw<ArgumentException>(() => new Schema().RegisterTypeMapping(typeof(int), typeof(int)));
        ex.ParamName.ShouldBe("graphType");
        ex.Message.ShouldStartWith("System.Int32' must be a GraphType (i.e. not CLR type like System.DateTime or System.String). You must specify GraphType type instead of CLR type.");
    }

    [Fact]
    public void RemapType_Generic_Calls_Schema_RemapType()
    {
        Type? capturedOriginal = null;
        Type? capturedNew = null;
        var mockSchema = new Mock<ISchema>(MockBehavior.Strict);
        mockSchema.Setup(x => x.RemapType(It.IsAny<Type>(), It.IsAny<Type>()))
            .Callback<Type, Type>((orig, @new) => { capturedOriginal = orig; capturedNew = @new; })
            .Verifiable();
        mockSchema.Object.RemapType<IntGraphType, MyCustomIntType>();
        capturedOriginal.ShouldBe(typeof(IntGraphType));
        capturedNew.ShouldBe(typeof(MyCustomIntType));
        mockSchema.Verify();
    }

    [Fact]
    public void RemapType_Null_OriginalType_Should_Throw()
    {
        Should.Throw<ArgumentNullException>(() => new Schema().RemapType(null!, typeof(IntGraphType)));
    }

    [Fact]
    public void RemapType_Null_NewType_Should_Throw()
    {
        Should.Throw<ArgumentNullException>(() => new Schema().RemapType(typeof(IntGraphType), null!));
    }

    [Fact]
    public void RemapType_NonGraphType_OriginalType_Should_Throw()
    {
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => new Schema().RemapType(typeof(int), typeof(IntGraphType)));
        ex.ParamName.ShouldBe("originalType");
    }

    [Fact]
    public void RemapType_NonGraphType_NewType_Should_Throw()
    {
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => new Schema().RemapType(typeof(IntGraphType), typeof(int)));
        ex.ParamName.ShouldBe("newType");
    }

    [Fact]
    public void RemapType_After_Initialization_Should_Throw()
    {
        var schema = new Schema { Query = new ObjectGraphType() };
        schema.Query.AddField(new FieldType { Name = "dummy", Type = typeof(StringGraphType) });
        schema.Initialize();
        Should.Throw<InvalidOperationException>(() => schema.RemapType(typeof(IntGraphType), typeof(MyCustomIntType)));
    }

    [Fact]
    public void RemapType_Remaps_Type_During_Initialization()
    {
        var query = new ObjectGraphType();
        query.Field<IntGraphType>("value");

        var schema = new Schema { Query = query };
        schema.RemapType<IntGraphType, MyCustomIntType>();
        schema.Initialize();

        // After remapping, the schema should use MyCustomIntType (named "Int") instead of IntGraphType
        schema.AllTypes["Int"].ShouldBeOfType<MyCustomIntType>();
    }

    [Fact]
    public void RemapType_Stores_Remappings()
    {
        var schema = new Schema { Query = new ObjectGraphType() };
        schema.Query.AddField(new FieldType { Name = "dummy", Type = typeof(StringGraphType) });
        schema.RemapType(typeof(IntGraphType), typeof(MyCustomIntType));

        var remappings = schema.TypeRemappings.ToList();
        remappings.Count.ShouldBe(1);
        remappings[0].originalType.ShouldBe(typeof(IntGraphType));
        remappings[0].newType.ShouldBe(typeof(MyCustomIntType));
    }

    private class MyCustomIntType : IntGraphType
    {
        public MyCustomIntType()
        {
            Name = "Int"; // Same name as IntGraphType to be a proper replacement
        }
    }
}
