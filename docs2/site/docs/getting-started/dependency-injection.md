# Dependency Injection

GraphQL .NET supports dependency injection through a `IServiceProvider` interface that is passed to the Schema class. Internally when trying to resolve a type the library will call the methods on this interface.

> The library resolves a `GraphType` only once and caches that type for the lifetime of the `Schema`.

The default implementation of `IServiceProvider` uses `Activator.CreateInstance`. `Activator.CreateInstance` requires that an object have a public parameterless constructor.

```csharp
public sealed class DefaultServiceProvider : IServiceProvider
{
    public object GetService(Type serviceType)
    {
        try
        {
            return Activator.CreateInstance(serviceType);
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to call Activator.CreateInstance. Type: {serviceType.FullName}", exception);
        }
    }
}
```

You can override the default implementation by passing a `IServiceProvider` to the constructor of your `Schema`.

```csharp
public class StarWarsSchema : GraphQL.Types.Schema
{
    public StarWarsSchema(IServiceProvider provider)
        : base(provider)
    {
        Query = provider.GetRequiredService<StarWarsQuery>();
        Mutation = provider.GetRequiredService<StarWarsMutation>();
    }
}
```

How you integrate this into your system will depend on the dependency injection framework you are using. `FuncServiceProvider` is provided for easy integration with multiple containers.

## ASP.NET Core

[See this example.](https://github.com/graphql-dotnet/examples/blob/8d5b7544006902f45b818010585b1ffa86ef446b/src/AspNetCoreCustom/Example/Startup.cs#L16-L34)

`Microsoft.Extensions.DependencyInjection` package used in ASP.NET Core already has support for `IServiceProvider` interface so no additional settings are required - just add your required dependencies:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
    services.AddSingleton<IDocumentWriter, DocumentWriter>();
    services.AddSingleton<StarWarsData>();
    services.AddSingleton<StarWarsQuery>();
    services.AddSingleton<StarWarsMutation>();
    services.AddSingleton<HumanType>();
    services.AddSingleton<HumanInputType>();
    services.AddSingleton<DroidType>();
    services.AddSingleton<CharacterInterface>();
    services.AddSingleton<EpisodeEnum>();
    services.AddSingleton<ISchema, StarWarsSchema>();
}
```

## Nancy TinyIoCContainer

```csharp
protected override void ConfigureApplicationContainer(TinyIoCContainer container)
{
    base.ConfigureApplicationContainer(container);

    container.Register((c, overloads) =>
    {
        return new StarWarsSchema(new FuncServiceProvider(c.Resolve));
    });
}
```

## SimpleContainer

```csharp
var container = new SimpleContainer();
container.Singleton(new StarWarsSchema(new FuncServiceProvider(container.Get)));
```

## Autofac

```csharp
protected override void Load(ContainerBuilder builder)
{
    builder
      .Register(c => new FuncServiceProvider(c.Resolve<IComponentContext>().Resolve))
      .As<IServiceProvider>()
      .InstancePerDependency();
}
```

## Castle Windsor

```csharp
public void Install(IWindsorContainer container, IConfigurationStore store)
{
    container.Register(
      Component
        .For<IServiceProvider>()
        .UsingFactoryMethod(k => k.Resolve)
    );
}
```
