namespace GraphQL.Tests.DI;

public class DefaultServiceProviderTests
{
    private readonly IServiceProvider _serviceProvider = new DefaultServiceProvider();

    [Fact]
    public void throws_for_null_service_type()
    {
        Should.Throw<ArgumentNullException>(() => _serviceProvider.GetService(null));
    }

    [Fact]
    public void returns_self_for_iserviceprovider()
    {
        _serviceProvider.GetService(typeof(IServiceProvider)).ShouldBe(_serviceProvider);
    }

    [Fact]
    public void returns_self_for_defaultserviceprovider()
    {
        _serviceProvider.GetService(typeof(DefaultServiceProvider)).ShouldBe(_serviceProvider);
    }

    [Fact]
    public void returns_null_for_interfaces()
    {
        _serviceProvider.GetService(typeof(IEnumerable<Class1>)).ShouldBeNull();
    }

    [Fact]
    public void returns_null_for_abstract_classes()
    {
        _serviceProvider.GetService(typeof(System.IO.TextReader)).ShouldBeNull();
    }

    [Fact]
    public void returns_null_for_static_classes()
    {
        _serviceProvider.GetService(typeof(Console)).ShouldBeNull();
    }

    [Fact]
    public void returns_null_for_generic_type_definitions()
    {
        _serviceProvider.GetService(typeof(List<>)).ShouldBeNull();
    }

    [Fact]
    public void returns_new_instance_for_generic_types()
    {
        _serviceProvider.GetService(typeof(List<int>)).ShouldBeOfType<List<int>>().ShouldNotBeNull();
    }

    [Fact]
    public void returns_new_instance_for_classes()
    {
        var obj1 = _serviceProvider.GetService(typeof(Class1)).ShouldBeOfType<Class1>().ShouldNotBeNull();
        var obj2 = _serviceProvider.GetService(typeof(Class1)).ShouldBeOfType<Class1>().ShouldNotBeNull();
        obj1.ShouldNotBe(obj2);
    }

    private class Class1
    {
    }
}
