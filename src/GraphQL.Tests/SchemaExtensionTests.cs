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
        Should.Throw<ArgumentNullException>(() => new Schema().RegisterTypeMappings(null));
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/3507
    [Fact]
    public void RegisterTypeMapping_With_GraphType_Instead_Of_ClrType_Should_Throw()
    {
        var ex = Should.Throw<ArgumentException>(() => new Schema().RegisterTypeMapping<IntGraphType, IntGraphType>());
        ex.ParamName.ShouldBe("clrType");
        ex.Message.ShouldStartWith("GraphQL.Types.IntGraphType' is already a GraphType (i.e. not CLR type like System.DateTime or System.String). You must specify CLR type instead of GraphType.");
    }
}
