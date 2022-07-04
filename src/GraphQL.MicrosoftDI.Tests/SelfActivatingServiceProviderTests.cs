using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GraphQL.MicrosoftDI.Tests;

public class SelfActivatingServiceProviderTests
{
    private readonly IServiceProvider _scopedServiceProvider1;
    private readonly IServiceProvider _scopedServiceProvider2;
    private readonly IServiceProvider _selfActivatingServiceProvider2;

    public SelfActivatingServiceProviderTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestSingleton>();
        services.AddScoped<TestScoped>();
        var rootServiceProvider = services.BuildServiceProvider();
        _scopedServiceProvider1 = rootServiceProvider.CreateScope().ServiceProvider;
        _scopedServiceProvider2 = rootServiceProvider.CreateScope().ServiceProvider;
        _selfActivatingServiceProvider2 = new SelfActivatingServiceProvider(_scopedServiceProvider2);
    }

    [Fact]
    public void requesting_iserviceprovider_returns_itself()
    {
        _selfActivatingServiceProvider2.GetRequiredService<IServiceProvider>().ShouldBe(_selfActivatingServiceProvider2);
    }

    [Fact]
    public void requesting_SelfActivatingServiceProvider_returns_itself()
    {
        _selfActivatingServiceProvider2.GetRequiredService<SelfActivatingServiceProvider>().ShouldBe(_selfActivatingServiceProvider2);
    }

    [Fact]
    public void prefers_pulling_from_service_provider()
    {
        var obj1 = _scopedServiceProvider1.GetRequiredService<TestSingleton>();
        var obj2 = _selfActivatingServiceProvider2.GetRequiredService<TestSingleton>();
        obj1.ShouldBe(obj2);
    }

    [Fact]
    public void pulls_from_scoped_service_provider()
    {
        var obj1 = _scopedServiceProvider1.GetRequiredService<TestScoped>();
        var obj2 = _scopedServiceProvider2.GetRequiredService<TestScoped>();
        var obj3 = _selfActivatingServiceProvider2.GetRequiredService<TestScoped>();
        obj1.ShouldNotBe(obj2);
        obj2.ShouldBe(obj3);
    }

    [Fact]
    public void creates_when_not_registered()
    {
        _selfActivatingServiceProvider2.GetRequiredService<TestUnregistered>();
    }

    [Fact]
    public void creates_when_not_registered_with_dependencies()
    {
        _selfActivatingServiceProvider2.GetRequiredService<TestUnregisteredWithDependencies>();
    }

    [Fact]
    public void fails_with_missing_dependencies()
    {
        Should.Throw<InvalidOperationException>(() => _selfActivatingServiceProvider2.GetRequiredService<TestUnregisteredWithMissingDependencies>());
    }

    [Fact]
    public void created_instances_are_not_disposed()
    {
        // if this test "fails", then our documentation needs to change. It does not necessarily indicate a fault.
        MyClass1 class1 = null;
        var services = new ServiceCollection();
        services.AddSingleton(services => class1 = new MyClass1());
        var provider = services.BuildServiceProvider();
        var myprovider = new SelfActivatingServiceProvider(provider);
        var class2 = myprovider.GetRequiredService<MyClass2>();
        provider.Dispose();
        (myprovider as IDisposable).ShouldBeNull(); // SelfActivatingServiceProvider does not yet support IDisposable
        class1.Disposed.ShouldBeTrue();
        class2.Disposed.ShouldBeFalse();
    }

    [Fact]
    public void unregistered_generic_types_return_null()
    {
        var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        mockServiceProvider.Setup(x => x.GetService(typeof(List<>))).Returns(null).Verifiable();
        var sasp = new SelfActivatingServiceProvider(mockServiceProvider.Object);
        sasp.GetService(typeof(List<>)).ShouldBeNull();
        mockServiceProvider.Verify();
    }

    private class TestSingleton
    {
    }

    private class TestScoped
    {
    }

    private class TestUnregistered
    {
    }

    private class TestUnregisteredWithDependencies
    {
        public TestUnregisteredWithDependencies(TestSingleton testSingleton)
        {
            if (testSingleton == null)
                throw new ArgumentNullException(nameof(testSingleton));
        }
    }

    private class TestUnregisteredWithMissingDependencies
    {
        public TestUnregisteredWithMissingDependencies(TestUnregistered testUnregistered)
        {
            _ = testUnregistered; // ignore compile warning about unused member
        }
    }

    public class MyClass1 : IDisposable
    {
        public bool Disposed { get; set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    public class MyClass2 : IDisposable
    {
        public bool Disposed { get; set; }

        public MyClass2(MyClass1 class1)
        {
            _ = class1;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
