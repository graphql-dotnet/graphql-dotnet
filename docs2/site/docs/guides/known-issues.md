# Known Issues / FAQ

## FAQ

### Is it possible to auto-generate classes from a schema?

This functionality is not provided by GraphQL.NET. See [issue #576](https://github.com/graphql-dotnet/graphql-dotnet/issues/576).

### Is it possible to auto-generate a graph type from a class?

Yes, via the `AutoRegisteringObjectGraphType`/`AutoRegisteringInputObjectGraphType` classes.
You can also configure auto-generated fields and auto-create enum types via the `EnumerationGraphType<>`
generic class.

Here is a sample of using an enumeration graph type:

```csharp
Field<ListGraphType<EnumerationGraphType<Episodes>>>("appearsIn").Description("Which movie they appear in.");
```

Here is a sample of an auto registering input graph type:

```csharp
class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

Field<StringGraphType>("addPerson")
    .Arguments<AutoRegisteringInputObjectGraphType<Person>>("value")
    .Resolve(context => {
        var person = context.GetArgument<Person>("value");
        db.Add(person);
        return "ok";
    });
```

Here is a sample of an auto registering object graph type that modifies some of the fields:

```csharp
class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime LastUpdated { get; set; }
}

class ProductGraphType : AutoRegisteringObjectGraphType<Product>
{
    public ProductGraphType()
        : base(x => x.LastUpdated)
    {
        GetField("Name").Description = "A short name of the product";
    }
}

Field<ListGraphType<ProductGraphType>>("products").Resolve(_ => db.Products);
```

Note that you may need to register the classes within your dependency injection framework:

```csharp
services.AddSingleton<EnumerationGraphType<Episodes>>();
services.AddSingleton<AutoRegisteringInputGraphType<Person>>();
services.AddSingleton<ProductGraphType>();
```

Alternatively, you can register open generic classes:

```csharp
services.AddSingleton(typeof(AutoRegisteringInputGraphType<>));
services.AddSingleton(typeof(AutoRegisteringObjectGraphType<>));
services.AddSingleton(typeof(EnumerationGraphType<>));
```

In the above sample, you would still need to register `ProductGraphType` separately.

### Is it possible to download/upload a file with GraphQL?

Files would need to be encoded in some form that is transmissible via JSON (e.g. Base64). If the file isn't part of some other
structured data result, it may not be a good candidate for a GraphQL API.

Note that Base64 is significantly less efficient bandwidth-wise than binary transfer, and you won't get an automatic browser
download prompt from receiving it.

If you are attempting to return pictures to be directly consumed in a web front-end, you can encode the picture as Base64 and
prepend a data URL tag (e.g. "`data:image/jpg;base64,`") which can be interpreted by common web browsers.

Similarly, if you are attempting a mutation to allow file uploading from a web browser, you can have a field resolver
accept a `StringGraphType` argument consisting of a data url with base64 encoded data.

Note that automatic conversion from Base64 string to byte array (but not byte array to Base64 string) is provided by
GraphQL.NET. This means you can use `GetArgument<byte[]>()` to retrieve a byte array from a field argument, provided that
the argument was a Base64 string.

### Can you use flag enumerations - enumerations marked with the `FlagsAttribute`?

Flag enumerations are not natively supported by the GraphQL specification. However,
you can provide a similar behavior by converting your enumeration values to and from
a list of enums. Here is a sample of some extension methods to facilitate this:

```csharp
public static class EnumExtensions
{
    public static IEnumerable<T> FromFlags<T>(this T value) where T : struct, Enum
        => Enum.GetValues(typeof(T)).Cast<T>().Distinct().Where(x => value.HasFlag(x));

    public static IEnumerable<T> FromFlags<T>(this T? value) where T : struct, Enum
        => value.HasValue ? value.Value.FromFlags() : null;

    public static T CombineFlags<T>(this IEnumerable<T> values) where T : struct, Enum
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        var enumType = typeof(T).GetEnumUnderlyingType();
        if (enumType == typeof(int))
            return (T)Enum.ToObject(typeof(T), values.Cast<int>().Aggregate((a, b) => a | b));
        // add support for uint/long/etc here
        throw new NotSupportedException("Enum type not supported");
    }
}

[Flags]
enum MyFlags
{
    Grumpy = 1,
    Happy = 2,
    Sleepy = 4,
}

// this returns the list ["GRUMPY", "HAPPY"]
Field<ListGraphType<EnumerationGraphType<MyFlags>>>("getFlagEnum")
    .Resolve(ctx => {
        var myFlags = MyFlags.Grumpy | MyFlags.Happy;
        return myFlags.FromFlags()
    });

// when calling convertEnumListToString(arg: [GRUMPY, HAPPY]), it returns the string "Grumpy, Happy"
Field<StringGraphType>("convertEnumListToString")
    .Argument<ListGraphType<EnumerationGraphType<MyFlags>>>("arg")
    .Resolve(ctx => ctx.GetArgument<IEnumerable<MyFlags>>("arg").CombineFlags().ToString());
```

### Can custom scalars serialize non-null data to a null value and vice versa?

Yes; let's say you want to write a custom serializer for date/time data types where it changes
strings of the format "MM-dd-yyyy" into `DateTime` values, and empty strings into null values. That
functionality is possible with a custom scalar.

Custom scalars transform external representations into internal representations and vice versa.
So an external representation might be `null` while the internal representation might be an
empty string. The reverse is also possible; an external representation of an empty string having
an internal representation of `null`.

However, field arguments' default values are stored in their local representation, with the exception
of a value of `null` indicates that the default value is not specified. So you cannot have a specified
default value with a non-null external representation and a null internal representation. This is a
limitation of GraphQL.NET.

### Should I use `AuthorizeAttribute` or the `AuthorizeWith` method?

`AuthorizeAttribute` is only for use with the schema-first or type-first syntax. `AuthorizeWith` is for use
with the code-first approach.

See [issue #68](https://github.com/graphql-dotnet/authorization/issues/68) and [issue #74](https://github.com/graphql-dotnet/authorization/issues/74)
within the [authorization](https://github.com/graphql-dotnet/authorization) package.

### Can descriptions be inherited if a graph type implements an GraphQL interface?

Yes; although descriptions directly set on the graph type take precedence.

### How can I use the data loader for a many-to-many relationship?

This is done within your database queries; it is not a function of the dataloader. Use the same
`CollectionBatchDataLoader` as you would for a one-to-many relationship; then when you are loading
data from your database within the fetch delegate, use an inner join to retrieve the proper data.

### Why does my saved `IResolveFieldContext` instance "change" after the field resolver executes?

The `IResolveFieldContext` instance passed to field resolvers is re-used at the completion of the resolver. Be sure not to
use this instance once the resolver finishes executing. To preserve a copy of the context, call `.Copy()` on the context
to create a copy that is not re-used. Note that it is safe to use the field context within asynchronous field resolvers,
data loaders and list fields. Once the asynchronous field resolver or data loader returns its final result, the context may be re-used.
Also, any calls to the configured `UnhandledExceptionDelegate` will receive a field context copy that will not be re-used,
so it is safe to preserve these instances without calling `.Copy()`.

## Known Issues

### IResolveFieldContext.HasArgument issue

`IResolveFieldContext.HasArgument` will return `true` for all arguments where `GetArgument` does not return `null`.
It cannot identify which arguments have been provided a `null` value compared to arguments which were not provided.
This issue should supposedly be resolved in version 4.

### Serialization of decimals does not respect precision

This one is `Newtonsoft.Json` specific issue. For more information see:
- https://github.com/JamesNK/Newtonsoft.Json/issues/1726
- https://stackoverflow.com/questions/21153381/json-net-serializing-float-double-with-minimal-decimal-places-i-e-no-redundant

As a workaround you may add `FixPrecisionConverter`:

```csharp
new NewtonsoftJson.DocumentWriter(settings =>
{
    settings.Converters.Add(new NewtonsoftJson.FixPrecisionConverter(true, true, true));
})
```

## Common Errors

### Synchronous operations are disallowed.

> InvalidOperationException: Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead

ASP.NET Core 3 does not by default allow synchronous reading/writing of input/output streams. When using the `Newtonsoft.Json` package,
you will need to set the `AllowSynchronousIO` property to `true`. The `System.Text.Json` package fully supports
asynchronous reading of json data streams and should not be a problem.

Here is the workaround for `Newtonsoft.Json`:

```csharp
// kestrel
services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

// IIS
 services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});
```

### Cannot resolve scoped service within graph type

> InvalidOperationException: Cannot resolve scoped service from root provider

The recommended lifetime for the schema and its graph types within a dependency injection
framework is the 'singleton' lifetime. This prevents the schema from having to be built
for every GraphQL request. Since the graph types are constructed at the same time as the
schema, it is not possible to register the graph types as scoped services while leaving
the schema as a singleton. Instead, you will need to pull your scoped services from within
the field resolver via the `IResolveFieldContext.RequestServices` property. Detailed
information on this technique, its configuration requirements, and alternatives are outlined
in the [Dependency Injection](../getting-started/dependency-injection.md) documentation.

It is also possible to register the schema and all its graph types as scoped services.
This is not recommended due to the overhead of building the schema for each request.

Note that concurrency issues typically arise when using scoped services with a parallel
execution strategy. Please read the section on this in the
[documentation](../getting-started/dependency-injection.md#scoped-services-with-a-singleton-schema-lifetime).

### Entity Framework concurrency issues

> InvalidOperationException: A second operation started on this context before a previous
> operation completed. This is usually caused by different threads using the same instance
> of DbContext. For more information on how to avoid threading issues with DbContext,
> see https://go.microsoft.com/fwlink/?linkid=2097913.

This problem is due to the fact that the default execution strategy for a query operation
is the `ParallelExecutionStrategy`, per the [spec](https://spec.graphql.org/October2021/#sec-Normal-and-Serial-Execution),
combined with the fact that you are using a shared instance of the Entity Framework
`DbContext`.

For instance, let's say the database context is registered as a scoped service (typical for EF),
and if you request the database context via the `IResolveFieldContext.RequestServices` property,
you will retrieve an instance of the database context that, although unique
to this request, is shared between all field resolvers within this request.

The easiest option is to change the execution strategy to `SerialExecutionStrategy`. Although
this would solve concurrency issues in this case, there may be an objectionable performance
degradation, since only a single field resolver can execute at a time.

A second option would be to change the database context lifetime to 'transient'. This means
that each time the database context was requested, it would receive a different copy, solving
the concurrency problems with GraphQL.NET's parallel execution strategy. However, if your
business logic layer passes EF-tracked entities through different services, this will not
work for you as each of the different services will not know about the tracked entities
passed from another service. Therefore, the database context must remain scoped.

Finally, you can create a scope within each field resolver that relies on Entity Framework
or your other scoped services. Please see the section on this in the
[dependency injection documentation](../getting-started/dependency-injection.md#scoped-services-with-a-singleton-schema-lifetime).

Also see discussion in [#1310](https://github.com/graphql-dotnet/graphql-dotnet/issues/1310) with related issues.

### Enumeration members' case sensitivity

Prior to GraphQL.NET version 5, enumeration values were case insensitive matches, which
did not meet the GraphQL specification. This has been updated to match the spec; to revert to the prior
behavior, please see [issue #3105](https://github.com/graphql-dotnet/graphql-dotnet/issues/3105#issuecomment-1109991628).

### Multiple instances of same graph type error

You may encounter errors of the following nature:

> A different instance of the GraphType 'WidgetGraphType' with the name 'Widget' has already been registered within the
> schema. Please use the same instance for all references within the schema, or use GraphQLTypeReference to reference a
> type instantiated elsewhere.

This is caused when the same graph type class has multiple distinct instances used within the schema. This is a change
in GraphQL.NET v7, and exists as protection to ensure that the schema is initialized properly. Note that this restriction
does not apply to scalars.

Below are some samples of what does not work in GraphQL.NET v7, along with solutions:

```csharp
// sample 1: manually creating instances without providing the same instance for each use
public class MyGraphType : ObjectGraphType
{
    public MyGraphType()
    {
        // creates an instance of WidgetGraphType
        Field("field1", new WidgetGraphType());
        // creates another instance of the same class
        Field("field2", new WidgetGraphType());

        // solution:
        Field<WidgetGraphType>("field1");
        Field<WidgetGraphType>("field2");
    }
}

// sample 2: adding an instance to a union graph type
public class MyUnionGraphType : UnionGraphType
{
    public MyUnionGraphType()
    {
        // creates an instance, which will be different than the one used elsewhere
        AddPossibleType(new WidgetGraphType());

        // solution:
        Type<WidgetGraphType>();
    }
}

// sample 3: pulling an instance from DI
public class MyUnionGraphType : UnionGraphType
{
    public MyUnionGraphType(WidgetGraphType widgetType)
    {
        // Since graph types are typically registered as transients, this reference to
        // WidgetGraphType will be different than other instances throughout the schema
        // and the following code does not work.
        AddPossibleType(widgetType);
        ResolveType = obj => obj switch
        {
            string => widgetType,
            _ => null,
        };

        // solution 1: register WidgetGraphType as a singleton manually with the DI provider

        // solution 2: remove WidgetGraphType from the constructor and use the following code:
        Type<WidgetGraphType>();
        ResolveType = obj => obj switch
        {
            string => new GraphQLTypeReference("Widget"), // reference by name
            _ => null,
        };
    }
}
```

However, within your schema, you may pull the query, mutation and/or subscription types from DI.
This is normal as those types are not typically referenced anywhere else in the schema.

```csharp
public class MySchema : Schema
{
    // correct implementation for the schema class
    public MySchema(IServiceProvider serviceProvider, MyQueryGraphType queryGraphType)
        : base(serviceProvider)
    {
        Query = queryGraphType;
    }
}
```

Similar restrictions apply if creating a dynamic schema. You will need to maintain a list of
graph type instances created by your schema and use those instances where necessary while
building the other graph types. You may also use `GraphQLTypeReference` as desired to reference
graph types by name.

```csharp
// create the graph types
var queryType = new ObjectGraphType() { Name = "Query" };
var widgetType = new ObjectGraphType() { Name = "Widget" };
var manufacturerType = new ObjectGraphType() { Name = "Manufacturer" };

// define the fields
widgetType.Field("Manufacturer", manufacturerType);
manufacturerType.Field("Widgets", new ListGraphType(widgetType));
queryType.Field("Widgets", new ListGraphType(widgetType));
queryType.Field("Manufacturers", new ListGraphType(manufactuerType));

// create the schema
var schema = new Schema();
schema.Query = queryType;
```
