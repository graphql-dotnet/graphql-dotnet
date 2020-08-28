# Known Issues / FAQ

## FAQ

### Is it possible to auto-generate classes from a schema?

This functionality is not provided by GraphQL.NET. See [issue #576](https://github.com/graphql-dotnet/graphql-dotnet/issues/576).

### Is it possible to auto-generate a graph type from a class?

Yes, via the `AutoRegisteringObjectGraphType` class. Similar classes are provided for input objects and enums.
You can also override auto-generated fields. See [issue #897](https://github.com/graphql-dotnet/graphql-dotnet/issues/897).

 (code samples here; maybe expand the paragraph)

### Is it possible to download/upload a file with GraphQL?

Files would need to be encoded in some form that is transmissible via JSON (e.g. Base64). If the file isn't part of some other
structured data result, it may not be a good candidate for a GraphQL API.

Note that Base64 is significantly less efficient bandwidth-wise than binary transfer, and you won't get an automatic browser
download prompt from receiving it.

If you are attempting to return pictures to be directly consumed in a web front-end, you can encode the picture as Base64 and
prepend a data URL tag (e.g. "`data:image/jpg;base64,`") which can be interpreted by common web browsers.

Similarly, if you are attempting a mutation to allow file uploading from a web browser, similarly, you can have a field resolver
accept a `StringGraphType` argument consisting of a data url with base64 encoded data.

### Other

 (todo: write brief paragraph for each item in the below list)

* Note about two authorization models - see [issue #897](https://github.com/graphql-dotnet/graphql-dotnet/issues/897)
* Note about descriptions inheritance - see authorization [issue #68](https://github.com/graphql-dotnet/authorization/issues/68) and [issue #74](https://github.com/graphql-dotnet/authorization/issues/74)
* Subscription authentication - see [issue #1244](https://github.com/graphql-dotnet/graphql-dotnet/issues/1244)
* Flag enums - see [issue #1666](https://github.com/graphql-dotnet/graphql-dotnet/issues/1666)
* `null` in scalars - see [issue #1542](https://github.com/graphql-dotnet/graphql-dotnet/issues/1542)

## Common Errors

### Synchronous operations are disallowed.

> InvalidOperationException: Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead

ASP.Net Core 3 does not by default allow synchronous reading/writing of input/output streams. When using the `Newtonsoft.Json` package,
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
the field resolver via the `IResolveFieldContext.RequestServices` property.  Detailed
information on this technique, its configuration requirements, and alternatives are outlined
in the [Dependency Injection](../getting-started/dependency-injection) documentation.

It is also possible to register the schema and all its graph types as scoped services.
This is not recommended due to the overhead of building the schema for each request.

Note that concurrency issues typically arise when using scoped services with a parallel
execution strategy. Please read the section on this in the
[documentation](../getting-started/dependency-injection#scoped-services-with-a-singleton-schema-lifetime).

### Entity Framework concurrency issues

> InvalidOperationException: A second operation started on this context before a previous
> operation completed. This is usually caused by different threads using the same instance
> of DbContext. For more information on how to avoid threading issues with DbContext,
> see https://go.microsoft.com/fwlink/?linkid=2097913.

This problem is due to the fact that the default execution strategy for a query operation
is the `ParallelExecutionStrategy`, per the [spec](https://spec.graphql.org/June2018/#sec-Normal-and-Serial-Execution),
combined with the fact that you are using a shared instance of the Entity Framework
`DbContext`.

For instance, let's say the database context is registered as a scoped service (typical for EF),
and if you request the database context via the `IResolveFieldContext.RequestServices` property,
you will retrieve an instance of the database context that, although unique
to this request, is shared between all field resolvers within this request.

The easiest option is to change the execution strategy to `SerialExecutionStrategy`. Although
this would solve concurrency issues in this case, there is a may be an objectionable performance
degredation, since only a single field resolver can execute at a time.

A second option would be to change the database context lifetime to 'transient'. This means
that each time the database context was requested, it would receive a different copy, solving
the concurrency problems with GraphQL.NET's parallel execution strategy. However, if your
business logic layer passes EF-tracked entities through different services, this will not
work for you as each of the different services will not know about the tracked entities
passed from another service. Therefore, the database context must remain scoped.

Finally, you can create a scope within each field resolver that relies on Entity Framework
or your other scoped services. Please see the section on this in the
[dependency injection documentation](../getting-started/dependency-injection#scoped-services-with-a-singleton-schema-lifetime).



