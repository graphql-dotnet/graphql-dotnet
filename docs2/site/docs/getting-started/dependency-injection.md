# Dependency Injection

GraphQL.NET supports dependency injection through a `IServiceProvider` interface that is passed to the Schema class. Internally when trying to resolve a type the library will call the methods on this interface.

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

# Schema Service Lifetime

Most dependency injection frameworks allow for specifying different service lifetimes for different
services. Although they may have different names with different frameworks, the three most common
lifetimes are as follows:

* **Transient** services are created every time they are injected or requested.
* **Scoped** services are created per scope. In a web application, every web request creates a new unique service scope. That means scoped services are generally created per web request.
* **Singleton** services are created per DI container. That generally means that they are created only one time per application and then used for whole the application life time.

It is highly recommended that the schema is registered as a singleton. This provides the best performance as
the schema does not need to be built for every request. As all graph types are constructed at the
same time as the schema, all graph types will effectively have a singleton lifetime, regardless
of how it is registered with the DI framework.

Scoped lifetime can be used to allow the schema and all its graph types access to the current DI scope.
This is not recommended; please see [Scoped Services](#scoped-services-with-a-singleton-schema-lifetime)
below. With scoped schemas, it is **required** that all its graph types are registered within the DI
framework as scoped or transient services.

Transient lifetime is also not recommended due to performance degradation. For schemas having a transient
lifetime, it is **required** that all its graph types are also registered within the DI framework as
transient services.

# Scoped services with a singleton schema lifetime

For reasons described above, it is recommended that the schema is registered as a singleton within
the dependency injection framework. However, this prevents including scoped services within the
constructor of the schema or your custom graph types.

To use scoped services (e.g. HttpContext scoped services in ASP.NET Core) you will need to pass
the scoped service provider into the `ExecutionOptions.RequestServices` property. Then within
any field resolver or field middleware, you can access the `IResolveFieldContext.RequestServices`
property to resolve types via the scoped service provider. Typical integration with ASP.NET Core
might look like this:

```csharp
var result = await _executer.ExecuteAsync(options =>
{
    options.Schema = schema;
    options.Query = request.Query;
    options.Inputs = request.Variables.ToInputs();
    options.RequestServices = context.RequestServices;
});
```

You could then call scoped services from within field resolvers as shown in the following example:

```csharp
public class StarWarsQuery : ObjectGraphType
{
    public StarWarsQuery()
    {
        Field<DroidType>(
            "hero",
            resolve: context => context.RequestServices.GetRequiredService<IDroidRepo>().GetDroid("R2-D2")
        );
    }
}
```

# Thread safety with scoped services

When using scoped services, be aware that most scoped services are not thread-safe. Therefore you will likely
need to use the `SerialExecutionStrategy` execution strategy, or write code to create a service scope
for the duration of the execution of the field resolver that requires a scoped service. For instance, with
Entity Framework Core, typically the database context is registered as a scoped service and obtained via
dependency injection. To continue to use the database context in the same manner with a singleton schema,
you would need to use a serial execution strategy, or create a scope within each field resolver that
requires database access, as shown in the following example:

```csharp
public class StarWarsQuery : ObjectGraphType
{
    public StarWarsQuery()
    {
        Field<DroidType>(
            "hero",
            resolve: context =>
            {
                using var scope = context.RequestServices.CreateScope();
                var services = scope.ServiceProvider;
                return services.GetRequiredService<MyDbContext>().Droids.Find(1);
            }
        );
    }
}
```

There are classes to assist with this within the [GraphQL.MicrosoftDI NuGet package](https://www.nuget.org/packages/GraphQL.MicrosoftDI).
Sample usage is as follows:

```csharp
public class MyGraphType : ObjectGraphType<Category>
{
    public MyGraphType()
    {
        Field("Name", context => context.Source.Name);
        Field<ListGraphType<ProductGraphType>>("Products")
            .ResolveScopedAsync((context, serviceProvider) => {
                var db = serviceProvider.GetRequiredService<MyDbContext>();
                return db.Products.Where(x => x.CategoryId == context.Source.Id).ToListAsync();
            });
    }
}
```

Be aware that using the service locator in this fashion described in this section could be considered an
Anti-Pattern. See [Service Locator is an Anti-Pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/).
Another approach to resolve scoped services is to use the SteroidsDI project, as described below.
Within the GraphQL.MicrosoftDI package, there is also a builder approach to adding scoped dependencies:

```csharp
public class MyGraphType : ObjectGraphType<Category>
{
    public MyGraphType()
    {
        Field("Name", context => context.Source.Name);
        Field<ListGraphType<ProductGraphType>>().Name("Products")
            .Resolve()
            .WithScope() // creates a service scope as described above; not necessary for serial execution
            .WithService<MyDbContext>()
            .ResolveAsync((context, db) => db.Products.Where(x => x.CategoryId == context.Source.Id).ToListAsync());
    }
}
```

## Using SteroidsDI

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
