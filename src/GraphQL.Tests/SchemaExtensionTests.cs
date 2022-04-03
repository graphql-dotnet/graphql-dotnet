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
}
