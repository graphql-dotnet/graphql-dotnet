using GraphQL.Types;

namespace GraphQL.Tests.Types;

public class ObjectGraphTypeTests
{
    private class TestInterface : InterfaceGraphType { }

    private class TypeWithInvalidName : ObjectGraphType
    {
        public TypeWithInvalidName()
        {
            Name = ":::";
        }
    }

    [GraphQLMetadata(Name = "testing")]
    private class TypeWithAttribute : ObjectGraphType { }

    [Fact]
    public void can_implement_interfaces()
    {
        var type = new ObjectGraphType();
        type.Interface(typeof(TestInterface));
        type.Interfaces.Count.ShouldBe(1);
    }

    private class TestPoco { }

    [Fact]
    public void can_implement_interfaces_in_derived_generic()
    {
        var type = new ObjectGraphType<TestPoco>();
        type.Interface(typeof(TestInterface));
        type.Interfaces.Count.ShouldBe(1);
    }

    [Fact]
    public void should_throw_on_invalid_graphtype_name()
    {
        var ex = new ArgumentOutOfRangeException("name", "A type name must match /^[_a-zA-Z][_a-zA-Z0-9]*$/ but ':::' does not.");
        // race condition with does_not_throw_with_filtering_nameconverter test
        try
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new TypeWithInvalidName()).Message.ShouldBe(ex.Message);
        }
        catch (ShouldAssertException)
        {
            System.Threading.Thread.Sleep(100); // wait a bit and retry
            Should.Throw<ArgumentOutOfRangeException>(() => new TypeWithInvalidName()).Message.ShouldBe(ex.Message);
        }
    }

    [Fact]
    public void should_ignore_graphqlmetadata_attribute()
    {
        var type = new TypeWithAttribute();
        type.Name.ShouldBe("TypeWithAttribute");
    }
}
