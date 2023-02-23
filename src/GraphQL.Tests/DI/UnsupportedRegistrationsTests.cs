using GraphQL.DI;
using GraphQL.Types;

namespace GraphQL.Tests.DI;

public class UnsupportedRegistrationTest1 : QueryTestBase<UnsupportedRegistrationSchema>
{
    public override void RegisterServices(IServiceRegister register)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => register.TryRegister(typeof(ISchema), typeof(UnsupportedRegistrationSchema), ServiceLifetime.Transient, (RegistrationCompareMode)42));
        register.Register(new UnsupportedRegistrationSchema(new DefaultServiceProvider()));
    }

    [Fact]
    public void Should_Initialize_Schema()
    {
        Schema.Initialize();
    }
}

public class UnsupportedRegistrationTest2 : QueryTestBase<UnsupportedRegistrationSchema>
{
    public override void RegisterServices(IServiceRegister register)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => register.TryRegister<ISchema, UnsupportedRegistrationSchema>(p => new UnsupportedRegistrationSchema(p), ServiceLifetime.Transient, (RegistrationCompareMode)42));
        register.Register(new UnsupportedRegistrationSchema(new DefaultServiceProvider()));
    }

    [Fact]
    public void Should_Initialize_Schema()
    {
        Schema.Initialize();
    }
}

public class UnsupportedRegistrationTest3 : QueryTestBase<UnsupportedRegistrationSchema>
{
    public override void RegisterServices(IServiceRegister register)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => register.TryRegister<ISchema>(new UnsupportedRegistrationSchema(new DefaultServiceProvider()), (RegistrationCompareMode)42));
        register.Register(new UnsupportedRegistrationSchema(new DefaultServiceProvider()));
    }

    [Fact]
    public void Should_Initialize_Schema()
    {
        Schema.Initialize();
    }
}

public class UnsupportedRegistrationSchema : Schema
{
    public UnsupportedRegistrationSchema(IServiceProvider provider)
        : base(provider)
    {
        Query = new DummyType();
    }
}
