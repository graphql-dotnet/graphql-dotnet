# Dependency Injection

GraphQL .NET supports dependency injection through a `IDependencyResolver` interface that is passed to the Schema class.  Internally when trying to resolve a type the library will call the methods on this interface.

> The library resolves a `GraphType` only once and caches that type for the lifetime of the `Schema`.

The default implementation of `IDependencyResolver` uses `Activator.CreateInstance`.  `Activator.CreateInstance` requires that an object have a public parameterless constructor.

```csharp
public class DefaultDependencyResolver : IDependencyResolver
{
    public T Resolve<T>()
    {
        return (T)Resolve(typeof(T));
    }

    public object Resolve(Type type)
    {
        return Activator.CreateInstance(type);
    }
}
```

You can override the default implementation by passing a `IDependencyResolver` to the constructor of your `Schema`.

```csharp
public class StarWarsSchema : GraphQL.Types.Schema
{
  public StarWarsSchema(IDependencyResolver resolver)
    : base(resolver)
  {
    Query = resolver.Resolve<StarWarsQuery>();
    Mutation = resolver.Resolve<StarWarsMutation>();
  }
}
```

How you integrate this into your system will depend on the dependency injection framework you are using.  `FuncDependencyResolver` is provided for easy integration with multiple containers.

## ASP.NET Core

[See this example.](https://github.com/graphql-dotnet/examples/blob/8d5b7544006902f45b818010585b1ffa86ef446b/src/AspNetCoreCustom/Example/Startup.cs#L16-L34)

```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddSingleton<IDependencyResolver>(s => new FuncDependencyResolver(s.GetRequiredService));

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
    return new StarWarsSchema(new FuncDependencyResolver(c.Resolve));
  });
}
```

## SimpleContainer

```csharp
var container = new SimpleContainer();
container.Singleton(new StarWarsSchema(new FuncDependencyResolver(container.Get)));
```

## Autofac

```csharp
protected override void Load(ContainerBuilder builder)
{
   builder
     .Register(c => new FuncDependencyResolver(c.Resolve<IComponentContext>().Resolve))
     .As<IDependencyResolver>()
     .InstancePerDependency();
}
```

## Castle Windsor

```csharp
public void Install(IWindsorContainer container, IConfigurationStore store)
{
   container.Register(
     Component
       .For<IDependencyResolver>()
       .UsingFactoryMethod(k => k.Resolve)
   );
}
```
