using GraphQL.DI;

namespace GraphQL.Tests;

internal static class ServiceRegisterExtensions
{
    public static void Transient<TService>(this IServiceRegister register)
    {
        register.Register(typeof(TService), typeof(TService), ServiceLifetime.Transient);
    }

    public static void Scoped<TService>(this IServiceRegister register)
    {
        register.Register(typeof(TService), typeof(TService), ServiceLifetime.Scoped);
    }

    public static void Singleton<TService>(this IServiceRegister register)
    {
        register.Register(typeof(TService), typeof(TService), ServiceLifetime.Singleton);
    }

    public static void Singleton<TService>(this IServiceRegister register, TService instance)
    {
        register.Register(typeof(TService), instance);
    }
}
