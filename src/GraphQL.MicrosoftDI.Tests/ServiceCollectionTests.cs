using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace GraphQL.MicrosoftDI.Tests;

public class ServiceCollectionTests
{
    private bool _disposed = false;
    private readonly ServiceCollection _serviceCollection = new();
    private readonly ITest _testObject;

    public ServiceCollectionTests()
    {
        var disposableMock = new Mock<ITest>(MockBehavior.Strict);
        disposableMock.Setup(x => x.Dispose()).Callback(() => _disposed = true);
        _testObject = disposableMock.Object;
    }

    private void Verify()
    {
        var services = _serviceCollection.BuildServiceProvider();
        services.GetRequiredService<ITest>().ShouldBe(_testObject);
        services.Dispose();
        _disposed.ShouldBeFalse();
    }

    [Fact]
    public void Register_InstanceIsNotDisposed()
    {
        _serviceCollection.AddSingleton(_testObject);
        Verify();
    }

    [Fact]
    public void TryRegister_InstanceIsNotDisposed()
    {
        _serviceCollection.TryAddSingleton(_testObject);
        Verify();
    }

    [Fact]
    public void Add_InstanceIsNotDisposed()
    {
        _serviceCollection.Add(new ServiceDescriptor(typeof(ITest), _testObject));
        Verify();
    }

    [Fact]
    public void Replace_InstanceIsNotDisposed()
    {
        _serviceCollection.Replace(new ServiceDescriptor(typeof(ITest), _testObject));
        Verify();
    }

    public interface ITest : IDisposable
    {
    }
}
