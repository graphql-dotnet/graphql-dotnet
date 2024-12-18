using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Utilities.Visitors;

public class TransitiveInterfaceVisitorTests
{
    [Fact]
    public void Should_Add_Transitive_Interfaces_To_Object_Type()
    {
        // Arrange
        var schema = new Schema();

        var interfaceC = new InterfaceGraphType { Name = "C" };
        var interfaceB = new InterfaceGraphType { Name = "B" };
        interfaceB.AddResolvedInterface(interfaceC);
        var interfaceA = new InterfaceGraphType { Name = "A" };
        interfaceA.AddResolvedInterface(interfaceB);

        var objectType = new ObjectGraphType { Name = "TestObject" };
        objectType.AddResolvedInterface(interfaceA);

        // Act
        TransitiveInterfaceVisitor.Instance.VisitObject(objectType, schema);

        // Assert
        objectType.ResolvedInterfaces.Count.ShouldBe(3);
        objectType.ResolvedInterfaces.ShouldContain(interfaceA);
        objectType.ResolvedInterfaces.ShouldContain(interfaceB);
        objectType.ResolvedInterfaces.ShouldContain(interfaceC);
    }

    [Fact]
    public void Should_Add_Transitive_Interfaces_To_Interface_Type()
    {
        // Arrange
        var schema = new Schema();

        var interfaceC = new InterfaceGraphType { Name = "C" };
        var interfaceB = new InterfaceGraphType { Name = "B" };
        interfaceB.AddResolvedInterface(interfaceC);
        var interfaceA = new InterfaceGraphType { Name = "A" };
        interfaceA.AddResolvedInterface(interfaceB);

        // Act
        TransitiveInterfaceVisitor.Instance.VisitInterface(interfaceA, schema);

        // Assert
        interfaceA.ResolvedInterfaces.Count.ShouldBe(2);
        interfaceA.ResolvedInterfaces.ShouldContain(interfaceB);
        interfaceA.ResolvedInterfaces.ShouldContain(interfaceC);
    }

    [Fact]
    public void Should_Handle_No_Interfaces()
    {
        // Arrange
        var schema = new Schema();
        var objectType = new ObjectGraphType { Name = "TestObject" };

        // Act
        TransitiveInterfaceVisitor.Instance.VisitObject(objectType, schema);

        // Assert
        objectType.ResolvedInterfaces.Count.ShouldBe(0);
    }

    [Fact]
    public void Should_Detect_Circular_References()
    {
        // Arrange
        var schema = new Schema();

        var interfaceA = new InterfaceGraphType { Name = "A" };
        var interfaceB = new InterfaceGraphType { Name = "B" };

        interfaceA.AddResolvedInterface(interfaceB);
        interfaceB.AddResolvedInterface(interfaceA);

        var objectType = new ObjectGraphType { Name = "TestObject" };
        objectType.AddResolvedInterface(interfaceA);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            TransitiveInterfaceVisitor.Instance.VisitInterface(interfaceA, schema))
            .Message.ShouldBe("'A' cannot implement interface 'B' because it creates a circular reference.");
    }

    [Fact]
    public void Should_Not_Add_Already_Implemented_Interfaces()
    {
        // Arrange
        var schema = new Schema();

        var interfaceC = new InterfaceGraphType { Name = "C" };
        var interfaceB = new InterfaceGraphType { Name = "B" };
        interfaceB.AddResolvedInterface(interfaceC);
        var interfaceA = new InterfaceGraphType { Name = "A" };
        interfaceA.AddResolvedInterface(interfaceB);

        var objectType = new ObjectGraphType { Name = "TestObject" };
        objectType.AddResolvedInterface(interfaceA);
        objectType.AddResolvedInterface(interfaceC); // Already directly implementing C

        // Act
        TransitiveInterfaceVisitor.Instance.VisitObject(objectType, schema);

        // Assert
        objectType.ResolvedInterfaces.Count.ShouldBe(3);
        objectType.ResolvedInterfaces.ShouldContain(interfaceA);
        objectType.ResolvedInterfaces.ShouldContain(interfaceB);
        objectType.ResolvedInterfaces.ShouldContain(interfaceC);
    }
}
