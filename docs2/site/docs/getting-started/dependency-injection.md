# Dependency Injection

GraphQL .NET supports dependency injection through a `IServiceProvider` interface that is passed to the Schema class. Internally when trying to resolve a type the library will call the methods on this interface.

> The library resolves a `GraphType` only once and caches that type for the lifetime of the `Schema`.

The default implementation of `IServiceProvider` uses `Activator.CreateInstance`. `Activator.CreateInstance` requires that an object have a public parameterless constructor.

```csharp
public sealed class DefaultServiceProvider : IServiceProvider
{
    public object GetService(Type serviceType)
    {
        if (serviceType == null)
            throw new ArgumentNullException(nameof(serviceType));

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

`Microsoft.Extensions.DependencyInjection` package used in ASP.NET Core already has support for resolving `IServiceProvider` interface so no additional settings are required - just add your required dependencies:

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

# Scoped Services

To use scoped services (e.g. HttpContext Scoped services in ASP.NET Core) you will either need to

* use [SteroidsDI](https://github.com/sungam3r/SteroidsDI)
* register a scoped `IServiceProvider` in the `UserContext` before running `IDocumentExecuter` ([anti-pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/))
* register a scope and provide custom Func<T> or factories (see how [SteroidsDI](https://github.com/sungam3r/SteroidsDI) is implemented)

## SteroidsDI

To use [SteroidsDI](https://github.com/sungam3r/SteroidsDI) with ASP.NET Core, add `Defer<>` and `IScopeProvider` in your `Startup.ConfigureServices`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...

    // Add SteroidsDI Open Generic Defer<> Factory Class
    services.AddDefer();

    // Add SteroidsDI IScopeProvider to use the AspNetCoreHttpScopeProvider
    // which internally uses the IHttpContextAccessor.HttpContext.RequestServices;
    services.AddHttpScope();

    ...
}
```

Then in your query graph types you can request services using `Defer<T>` to be injected via DI,
 which will be evaluated at runtime to their relevant Scoped services, `T` has been registered with a `Scoped` DI lifetime:

```csharp
public class StarWarsQuery : ObjectGraphType
{
  // #1 - Add dependencies using Defer<T>
  public StarWarsQuery(Defer<IDroidRepo> repoFactory)
  {
    Field<DroidType>(
      "hero",

      // #2 Resolve dependencies using current scope provider
      resolve: context => repoFactory.Value.GetDroid("R2-D2")

    );
  }
}
```

1. Add `Defer<T>` to be injected by the dependency injection container. This is a factory which upon calling `Defer.Value` will resolve the requested service using any currently registered scope provider (e.g. `AspNetCoreHttpScopeProvider`)
2. Use the `Defer<T>` factory class to resolve the requested dependency using any currently registered scope provider. In our case it will attempt to use the `IHttpContextAccessor.HttpContext.RequestServices` which is the ASP.NET Core Scoped `IServiceProvider` in order to resolve the dependency.

## UserContext Scoped IServiceProvider

**Be Aware**: Exposing the service locator is considered an Anti-Pattern. See [Service Locator is an Anti-Pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/)

You can add the relevant scoped `IServiceProvider` when creating the `UserContext` in order to run `IDocumentExecuter`.

E.g. In ASP.NET Core [GraphQl.Harness](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/GraphQL.Harness) example, you could add `IServiceProvider` to the [GraphQLUserContext](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL.Harness/GraphQLUserContext.cs) class, and then set it to the `HttpContext.RequestServices` when configuring the `GraphQLSettings.BuildUserContext`


1. Add ServiceProvider to the UserContext

    ```csharp
    public class GraphQLUserContext: Dictionary<string, object>
    {
        ...
        IServiceProvider ServiceProvider { get; set; }
        ...
    }
    ```

2. Set it the the HttpContext.RequestServices

    ```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<GraphQLSettings>(settings => settings.BuildUserContext = ctx => new GraphQLUserContext
        {
            User = ctx.User,
            ServiceProvider = ctx.RequestServices
        });
    }
    ```

3. Use it in resolvers

    ```csharp
    public class StarWarsQuery : ObjectGraphType<object>
    {
        public StarWarsQuery()
        {
            ...
            Field<CharacterInterface>("hero",
                resolve: context =>
                    ((GraphQLUserContext)context.UserContext)
                    .ServiceProvider.GetRequiredService<IDroidRepo>()
                    .GetDroidByIdAsync("3")
            );

            Field<CharacterInterface>("hero", resolve: context => context);
        }
    }
    ```

 You could always create an extension method:

 ```csharp
public static class GraphQLExtensions
{
    public static T GetRequiredService<T>(this IResolveFieldContext context) =>
        ((GraphQLUserContext)context.UserContext).ServiceProvider.GetRequiredService<T>();
}
 ```

and use it as such:

```csharp
Field<CharacterInterface>("hero",
            resolve: context =>
                context
                .GetRequiredService<IDroidRepo>()
                .GetDroidByIdAsync("3")
        );
```
