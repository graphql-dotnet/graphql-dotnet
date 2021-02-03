# Migrating from v3.x to v4.x

## New Features

### Improved Performance

GraphQL.NET 4.0 has been highly optimized, typically executing queries at least 50% faster while also providing a 65% memory reduction. Small queries have been measured to run twice as fast as they previously ran. A cached query executor is also provided, which can reduce execution time another 20% once the query has been parsed (disabled by default). Variable parsing is also improved to run about 50% faster, and schema build time is now about 20x faster than previously and requires 1/25th the amount of memory.

See the [Document Caching](https://graphql-dotnet.github.io/docs/guides/document-caching) guide to enable document caching.

To facilitate the performance changes, many changes were made to the API that may affect you if you have built custom execution strategies, scalars, parser, or similar core components. Please see the complete list of breaking changes below.

### Input Object Custom Deserializers (aka resolver)

You can now add code to `InputObjectGraphType` descendants to build an object from the collected argument fields. The new `ParseDictionary` method is called when variables are being parsed or `GetArgument` is called, depending on if the argument is stored within variables or as a literal. The method is passed a dictionary containing the input object's fields and deserialized values.

By default, for `InputObjectGraphType<TSourceType>` implementations, the dictionary is passed to `ObjectExtensions.ToObject` in order to convert the dictionary to an object of `TSourceType`. You can override the method to have it return an instance of any appropriate type.

Below is a sample which sets a default value for an unsupplied field (this could be done with a default value set on the field, of course) and converts the name to uppercase:

```cs
public class HumanInputType : InputObjectGraphType
{
    public HumanInputType()
    {
        Name = "HumanInput";
        Field<NonNullGraphType<StringGraphType>>("name");
        Field<StringGraphType>("homePlanet");
    }

    public override object ParseDictionary(IDictionary<string, object> value)
    {
        return new Human
        {
            Name = ((string)value["name"]).ToUpper(),
            HomePlanet = value.TryGetValue("homePlanet", out var homePlanet) ? (string)homePlanet : "Unknown",
            Id = null,
        };
    }
}
```

Note that pursuant to GraphQL specifications, if a field is optional, not supplied, and has no default, it will not be in the dictionary.

For untyped `InputObjectGraphType` classes, like shown above, the default behavior of `ParseDictionary` will be to return the dictionary. `GetArgument<T>` will still attempt to convert a dictionary to the requested type via `ObjectExtensions.ToObject` as it did before.

### Experimental Features / Applied Directives

> Ability to apply directives to the schema elements and expose user-defined meta-information via introspection - `schema.EnableExperimentalIntrospectionFeatures()`.
> See https://github.com/graphql/graphql-spec/issues/300 for more information.

(sungram3r todo)

### Comparer

> New property `GraphQL.Introspection.ISchemaComparer ISchema.Comparer { get; set; }`

(todo: update title, write descrption, add sample)

### ArrayPool

> New property `IResolveFieldContext.ArrayPool`

(todo: update title, write description, add sample)

### Global Switches

> `GlobalSwitches` - new global options for configuring GraphQL execution

(todo: write descrption of switches, where to configure, add samples)

### Authorization Extension Methods

> Extension methods to configure authorization requirements for GraphQL elements: types, fields, schema.

(todo: write more description about how it interacts with the other libraries, add simple sample)

### Other Features

* New method `IParentExecutionNode.ApplyToChildren`

## Breaking Changes

### Properties moved to Schema (rename title)

`NameConverter`, `SchemaFilter` and `FieldMiddleware` have been removed from `ExecutionOptions` and are now properties on the `Schema`.
These properties can be set in the constructor of the `Schema` instance, or within your DI composition root, or at any time before
any query is executed. Once a query has been executed, changes to these fields is not allowed, and adding middleware via the field middleware
builder has no effect.

### Middleware

> The signature of `IFieldMiddlewareBuilder.Use` has been changed to remove the schema from delegate. Since the schema is now known, there is no
> need for it to be passed to the middleware builder.

> The middleware `Use<T>` extension method has been removed. Please use the `Use` method with a middleware instance instead.

(todo: confirm description, add sample of required changes)

### GetRequiredService

> `GraphQL.Utilities.ServiceProviderExtensions` has been made internal. This affects usages of its extension method `GetRequiredService`.
> Instead, reference the `Microsoft.Extensions.DependencyInjection.Abstractions` NuGet package and use the extension method from the
> `Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions` class.

(todo: confirm description, maybe add sample change to using statement)

### Default Sort Order of Introspection Query Results

By default fields returned by introspection query are no longer sorted by their names. `LegacyV3SchemaComparer` can be used to switch to the old behavior.

```c#
    /// <summary>
    /// Default schema comparer for GraphQL.NET v3.x.x.
    /// By default only fields are ordered by their names within enclosing type.
    /// </summary>
    public sealed class LegacyV3SchemaComparer : DefaultSchemaComparer
    {
        private static readonly FieldByNameComparer _instance = new FieldByNameComparer();

        private sealed class FieldByNameComparer : IComparer<IFieldType>
        {
            public int Compare(IFieldType x, IFieldType y) => x.Name.CompareTo(y.Name);
        }

        /// <inheritdoc/>
        public override IComparer<IFieldType> FieldComparer(IGraphType parent) => _instance;
    }
```

(todo: add sample line of code of how to set the comparer in the schema)

### `IResolveFieldContext` Re-use

The `IResolveFieldContext` instance passed to field resolvers is re-used at the completion of the resolver. Be sure not to
use this instance once the resolver finishes executing. To preserve a copy of the context, call `.Copy()` on the context
to create a copy that is not re-used. Note that it is safe to use the field context within asynchronous field resolvers and
data loaders. Once the asynchronous field resolver or data loader returns its final result, the context will be cleared and may be re-used.
Also, any calls to the configured `UnhandledExceptionDelegate` will receive a field context copy that will not be re-used,
so it is safe to preserve these instances without calling `.Copy()`.

### Subscriptions Moved to Separate Project

> Subscriptions implementation (`SubscriptionExecutionStrategy`) has been moved into `GraphQL.SystemReactive` project and default document executer throws `NotSupportedException`.

(todo: revise description with reference to proper nuget package, describe changes required to project, etc)

### API Cleanup

* `GraphQL.Instrumentation.StatsReport` and its associated classes have been removed. Please copy the source code into your project if you require these classes.
* `LightweightCache.First` method has been removed.
* `IGraphType.CollectTypes` method has been removed.
* `ExecutionHelper.SubFieldsFor` method has been removed.
* `NodeExtensions`, `AstNodeExtensions` classes have been removed.
* `CoreToVanillaConverter` class became `static` and most of its members have been removed.
* `GraphQL.Language.AST.Field.MergeSelectionSet` method has been removed.
* `CoreToVanillaConverter.Convert` method now requires only one `GraphQLDocument` argument.
* `TypeCollectionContext` class is now internal, also all methods with this parameter in `GraphTypesLookup` are private.
* `GraphQLTypeReference` class is now internal, also `GraphTypesLookup.ApplyTypeReferences` is now private.
* `IHaveDefaultValue.Type` has been moved to `IProvideResolvedType.Type`
* `ErrorLocation` struct became `readonly`.
* `DebugNodeVisitor` class has been removed.
* Most methods and classes within `OverlappingFieldsCanBeMerged` are now private.
* `EnumerableExtensions.Apply` for dictionaries has been removed.
* `ISubscriptionExecuter` has been removed.
* `EnterLeaveListener` has been removed and the signatures of `INodeVisitor.Enter` and `INodeVisitor.Leave` have changed. `NodeVisitors` class has been added in its place.
* `TypeInfo.GetAncestors()` has been changed to `TypeInfo.GetAncestor(int index)`
* Various methods within `StringUtils` have been removed; please use extension methods within `StringExtensions` instead.
* `GraphTypesLookup` has been renamed to `SchemaTypes` with a significant decrease in public APIs 
* `ExecutionHelper.GetVariableValue` has been removed, and the signature for `ExecutionHelper.CoerceValue` has changed.
* Removed `TypeExtensions.As`
* `ExecutionHelper.CollectFields` method was moved into `Fields` class and renamed to `CollectFrom`
* `ISchema.FindDirective`, `ISchema.RegisterDirective`, `ISchema.RegisterDirectives` methods were moved into `SchemaDirectives` class
* `ISchema.FindType` method was moved into `SchemaTypes[string typeName]` indexer
* Some of the `ISchemaNodeVisitor` methods have been changes to better support schema traversal
* `SourceLocation`, `NameNode` and `BasicVisitor` have been changed to a `readonly struct`.
* `ObjectExtensions.GetInterface` has been removed along with two overloads of `GetPropertyValue`.
* `void INode.Visit<TState>(System.Action<INode, TState> action, TState state)` method has been added.
* Various `IEnumerable<T>` properties on schema and graph types have been changed to custom collections: `SchemaDirectives`, `SchemaTypes`, `TypeFields`, `PossibleTypes`, `ResolvedInterfaces`
* `INode.IsEqualTo` and related methods have been removed.
* `ApolloTracing.ConvertTime` is now private and `ResolverTrace.Path` does not initialize an empty list when created.

### `ExecutionOptions.EnableMetrics` is disabled by default

To enable metrics, please set the option to `true` before executing the query.

(todo: add sample)

### GraphQL Member Descriptions

> By default, descriptions for fields, types, enums, and so on are not pulled from xml comments unless the corresponding global flag is enabled.

Note that to improve performance, by default GraphQL.NET 4.0 does not pull descriptions for fields from xml comments as it did in 3.x. To re-enable that functionality, see [Global Switches](#Global-Switches) above.

(todo: update/consolidate description)

### Changes to `IResolveFieldContext.Arguments`

`IResolveFieldContext.Arguments` now returns an `IDictionary<string, ArgumentValue>` instead of `IDictionary<string, object>` so that it
can be determined if the value returned is a default value or if it is a specified literal or variable.

`IResolveFieldContext.HasArgument` now returns `false` when `GetArgument` returns a field default value. Note that if a variable is specified,
and the variable resolves to its default value, then `HasArgument` returns `true` (since the field argument has successfully resolved to a variable
specified by the query).

### Metadata is Not Thread Safe

> `IProvideMetadata.Metadata` is now `Dictionary` instead of `ConcurrentDictionary`, and is not thread safe anymore

(todo: update description, add description of how to lock on type, not dictionary)

### Other Breaking Changes

* GraphQL.NET now uses GraphQL-Parser v7 with new memory model taking advantage of `System.Memory` APIs.
* When used, Apollo tracing will now convert the starting timestamp to UTC so that `StartTime` and `EndTime` are properly serialized as UTC values.
* `Connection<TNode, TEdge>.TotalCount` has been changed from an `int` to an `int?`. This allows for returning `null` indicating that the total count is unknown.
* `InputObjectGraphType.ParseDictionary` has been added so that customized deserialization behavior can be specified for input objects (aka input resolvers).
  If `InputObjectGraphType<T>` is used, and `GetArgument<T>` is called with the same type, no behavior changes will occur by default.
  If `InputObjectGraphType<T>` is used, but `GetArgument<T>` is called with a different type, coercion may fail. Override `ParseDictionary`
  to force resolving the input object to the correct type.
