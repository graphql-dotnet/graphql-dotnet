using GraphQL.Builders;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GraphQL.MicrosoftDI.Tests;

public class ScopedContextBase
{
    protected readonly ResolveFieldContext _scopedContext;
    protected readonly ResolveConnectionContext<object> _scopedConnectionContext;
    protected readonly Mock<IServiceProvider> _requestServicesMock;
    protected readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    protected readonly Mock<IServiceScope> _scopedServiceScopeMock;
    protected readonly Mock<IServiceProvider> _scopedServiceProviderMock;
    protected readonly IServiceProvider _scopedServiceProvider;

    public ScopedContextBase()
    {
        _scopedServiceProviderMock = new Mock<IServiceProvider>(MockBehavior.Strict);
        _scopedServiceProvider = _scopedServiceProviderMock.Object;
        _scopedServiceScopeMock = new Mock<IServiceScope>(MockBehavior.Strict);
        _scopedServiceScopeMock.Setup(x => x.ServiceProvider).Returns(_scopedServiceProvider).Verifiable();
        _scopedServiceScopeMock.Setup(x => x.Dispose()).Verifiable();
        var scopedServiceScope = _scopedServiceScopeMock.Object;
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopedServiceScope).Verifiable();
        var serviceScopeFactory = _serviceScopeFactoryMock.Object;
        _requestServicesMock = new Mock<IServiceProvider>(MockBehavior.Strict);
        _requestServicesMock.Setup(x => x.GetService(It.Is<Type>(t => t == typeof(IServiceScopeFactory)))).Returns(serviceScopeFactory).Verifiable();
        var requestServices = _requestServicesMock.Object;
        _scopedContext = new ResolveFieldContext
        {
            RequestServices = requestServices
        };
        _scopedConnectionContext = new ResolveConnectionContext<object>(_scopedContext, false, default);
    }

    public void VerifyScoped()
    {
        _scopedServiceScopeMock.Verify();
        _serviceScopeFactoryMock.Verify();
        _requestServicesMock.Verify();
        _scopedServiceProviderMock.Verify();
    }
}
