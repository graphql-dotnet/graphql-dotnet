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

## Dependency Injection Registration Helpers

GraphQL.NET provides an `IGraphQLBuilder` interface which encapsulates the configuration methods of a dependency injection framework, to provide an
abstract method of configuring a dependency injection framework to work with GraphQL.NET. This interface is provided through a configuration delegate
from a DI-provider-specific setup method (typically called `AddGraphQL()`), at which point you can call extension methods on the interface to
configure this library. A simple example is below:

```csharp
services.AddGraphQL(builder => builder
    .AddSystemTextJson()
    .AddSchema<MySchema>());
```

The interface also allows configuration of the schema during initialization, and configuration of the execution at runtime. In this manner, adding
middleware, for example, is as simple as calling `.AddMiddleware<MyMiddlware>()` and does not require the middleware to be added into the schema
configuration.

The `AddGraphQL()` method will register default implementations of the following services within the dependency injection framework:

* `IDocumentExecuter`
* `IDocumentBuilder`
* `IDocumentValidator`
* `IComplexityAnalyzer` - which is not used unless configured within `ExecutionOptions`
* `IErrorInfoProvider`
* `IDocumentCache` - an implemenation which does not cache documents
* `IExecutionStrategySelector` - which does not support subscriptions by default

A list of the available extension methods is below:

| Method    | Description / Notes | Library |
|-----------|---------------------|---------|
| `AddAutoClrMappings`    | Configures unmapped CLR types to use auto-registering graph types | |
| `AddAutoSchema`         | Registers a schema based on CLR types | |
| `AddClrTypeMappings`    | Scans the specified assembly for graph types intended to represent CLR types and registers them within the schema | |
| `AddComplexityAnalyzer` | Enables the complexity analyzer and configures its options | |
| `AddDataLoader`         | Registers classes necessary for data loader support | GraphQL.DataLoader |
| `AddDocumentCache<>`    | Registers the specified document caching service | |
| `AddDocumentExecuter<>` | Registers the specified document executer; useful when needed to change the execution strategy utilized | |
| `AddDocumentListener<>` | Registers the specified document listener and configures execution to use it | |
| `AddErrorInfoProvider`  | Registers a custom error info provider or configures the default error info provider | |
| `AddExecutionStrategy`  | Registers an `ExecutionStrategyRegistration` for the selected execution strategy and operation type | |
| `AddExecutionStrategySelector` | Registers the specified execution strategy selector | |
| `AddGraphTypes`         | Scans the specified assembly for graph types and registers them within the DI framework | |
| `AddGraphTypeMappingProvider` | Registers a graph type mapping provider for unmapped CLR types | |
| `AddMemoryCache`        | Registers the memory document cache and configures its options | GraphQL.MemoryCache |
| `AddMetrics`            | Registers and enables metrics depending on the supplied arguments | |
| `AddMiddleware<>`       | Registers the specified middleware and configures it to be installed during schema initialization | |
| `AddNewtonsoftJson`     | Registers the serializer that uses Newtonsoft.Json as its underlying JSON serialization engine | GraphQL.NewtonsoftJson |
| `AddSchema<>`           | Registers the specified schema | |
| `AddSelfActivatingSchema<>` | Registers the specified schema which will create instances of unregistered graph types during initialization | |
| `AddSerializer<>`       | Registers the specified serializer | |
| `AddSystemTextJson`     | Registers the serializer that uses System.Text.Json as its underlying JSON serialization engine | GraphQL.SystemTextJson |
| `AddValidationRule<>`   | Registers the specified validation rule and configures it to be used at runtime | |
| `ConfigureExecutionOptions` | Configures execution options at runtime | |
| `ConfigureSchema`       | Configures schema options when the schema is initialized | |
| `Configure<TOptions>`   | Used by extension methods to configures an options class within the DI framework | |

The above methods will register the specified services typically as singletons unless otherwise specified. Graph types and middleware are registered
as transients so that they will match the schema lifetime. So with a singleton schema, all services are effectively singletons.

Custom `IGraphQLBuilder` extension methods typically rely on the `Services` property of the builder in order to register services
with the underlying dependency injection framework. The `Services` property returns a `IServiceRegister` interface which has these methods:

| Method         | Description |
|----------------|-------------|
| `Register`     | Registers a service within the DI framework replacing existing registration if needed |
| `TryRegister`  | Registers a service within the DI framework if it has not already been registered |

To use the `AddGraphQL` method, you will need to install the proper nuget package for your DI provider. See list below:

| DI Provider | Nuget Package |
|-------------|---------------|
| Microsoft.Extensions.DependencyInjection | GraphQL.MicrosoftDI |

## ASP.NET Core

[See this example.](https://github.com/graphql-dotnet/examples/blob/8d5b7544006902f45b818010585b1ffa86ef446b/src/AspNetCoreCustom/Example/Startup.cs#L16-L34)

`Microsoft.Extensions.DependencyInjection` package used in ASP.NET Core already has support for resolving `IServiceProvider` interface so no additional settings are required - just add your required dependencies:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
    services.AddSingleton<IGraphQLSerializer, GraphQLSerializer>();
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

To avoid having to register all of the individual graph types in your project, you can
import the [GraphQL.MicrosoftDI NuGet package](https://www.nuget.org/packages/GraphQL.MicrosoftDI)
package and utilize the `SelfActivatingServiceProvider` wrapper as follows:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<ISchema, StarWarsSchema>(services => new StarWarsSchema(new SelfActivatingServiceProvider(services)));
}
```

If you previously pulled in your query, mutation and/or subscription classes via dependency injection, you will need
to manually pull in those dependencies from the `SelfActivatingServiceProvider` via `GetRequiredService` as follows:

```csharp
public class StarWarsSchema : Schema
{
    public StarWarsSchema(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        Query = serviceProvider.GetRequiredService<StarWarsQuery>();
        Mutation = serviceProvider.GetRequiredService<StarWarsMutation>();
    }
}
```

No other graph types will need to be registered. Graph types will only be instantiated once, during schema initialization
as usual. Graph types can also pull in any services registered with dependency injection as usual.

Note that if any of the graph types directly or indirectly implement `IDisposable`, be sure to register those types with your dependency
injection provider, or their `Dispose` methods will not be called. Any dependencies of graph types that implement
`IDisposable` will be disposed of properly, regardless of whether the graph type is registered within the service provider.

You can also use the `.AddGraphTypes()` builder method to scan the calling or specified assembly for classes that implement
`IGraphType` and register them all as transients within the service provider. Mark your class with `DoNotRegisterAttribute` if you
want to skip registration.

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

> It is _highly_ recommended that the schema is registered as a singleton. As all graph types are constructed at the
same time as the schema, all graph types will effectively have a singleton lifetime, regardless
of how it is registered with the DI framework. This is most performant approach. Having a scoped schema can degrade performance
by a huge margin. For instance, even a small schema execution can slow down by 100x, and much more with a large schema.

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
    options.Schema = _schema;
    options.Query = request.Query;
    options.Variables = _serializer.Deserialize<Inputs>(request.Variables); // IGraphQLTextSerializer from DI
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
            .ResolveScopedAsync(context => {
                var db = context.RequestServices.GetRequiredService<MyDbContext>();
                return db.Products.Where(x => x.CategoryId == context.Source.Id).ToListAsync();
            });
    }
}
```

In this case `context.RequestServices` will be an `IServiceProvider` in a newly created scope.

Be aware that using the service locator in this fashion described in this section could be considered an
Anti-Pattern. See [Service Locator is an Anti-Pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/).
However, the performance benefits far outweigh the anti-pattern idealogy, when compared to creating a scoped schema.

Within the `GraphQL.MicrosoftDI` package, there is also a builder approach to adding scoped dependencies.
This makes for a concise and declarative approach. Each field clearly states the services it needs
and thereby, the anti-pattern argument does not apply anymore.

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

Another approach to resolve scoped services is to use the SteroidsDI project, as described below.

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
