using GraphQL.DI;
using GraphQL.Types;

namespace GraphQL.Tests.DI;

public class UnsupportedRegistrationTest1 : QueryTestBase<Schema>
{
    public override void RegisterServices(IServiceRegister register)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => register.TryRegister(typeof(ISchema), typeof(Schema), ServiceLifetime.Transient, (RegistrationCompareMode)42));
        register.Register(new Schema(new DefaultServiceProvider()));
    }

    [Fact]
    public void Should_Initialize_Schema()
    {
        Schema.Initialize();
    }
}

public class UnsupportedRegistrationTest2 : QueryTestBase<Schema>
{
    public override void RegisterServices(IServiceRegister register)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => register.TryRegister<ISchema, Schema>(p => new Schema(), ServiceLifetime.Transient, (RegistrationCompareMode)42));
        register.Register(new Schema(new DefaultServiceProvider()));
    }

    [Fact]
    public void Should_Initialize_Schema()
    {
        Schema.Initialize();
    }
}

public class UnsupportedRegistrationTest3 : QueryTestBase<Schema>
{
    public override void RegisterServices(IServiceRegister register)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => register.TryRegister<ISchema>(new Schema(), (RegistrationCompareMode)42));
        register.Register(new Schema(new DefaultServiceProvider()));
    }

    [Fact]
    public void Should_Initialize_Schema()
    {
        Schema.Initialize();
    }
}
