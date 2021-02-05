# Migrating from v3.x to v4.x

## New Features

* Extension methods to configure authorization requirements for GraphQL elements: types, fields, schema.
* New property `GraphQL.Introspection.ISchemaComparer ISchema.Comparer { get; set; }`
* New property `IResolveFieldContext.ArrayPool`
* New method `IParentExecutionNode.ApplyToChildren`
* Document caching supported via `IDocumentCache` and a default implementation within `DefaultDocumentCache`.
  Within the `GraphQL.Caching` nuget package, a memory-backed implementation is available which is backed by `Microsoft.Extensions.Caching.Memory.IMemoryCache`.
* `ExecutionOptions.EnableMetrics` is disabled by default
* `GlobalSwitches` - new global options for configuring GraphQL execution
* Ability to apply directives to the schema elements and expose user-defined meta-information via introspection - `schema.EnableExperimentalIntrospectionFeatures()`.
  See https://github.com/graphql/graphql-spec/issues/300 for more information.
* Support for repeatable directives and ability to expose `isRepeatable` field via introspection - `schema.ExperimentalFeatures.RepeatableDirectives`.
* Schema validation on initialize and better support for schema traversal via `ISchemaNodeVisitor`
 
## Breaking Changes

* GraphQL.NET now uses GraphQL-Parser v7 with new memory model
* `ExecutionResult.Data` format breaking changes. Now objects are serialized as `ObjectProperty` arrays, not object dictionaries.
  Both `GraphQL.NewtonsoftJson` and `GraphQL.SystemTextJson` serializers received the necessary changes to produce the same JSON as before.
  However, consumers using `ExecutionResult` instances directly most likely will not work correctly.
* `NameConverter`, `SchemaFilter` and `FieldMiddleware` have been removed from `ExecutionOptions` and are now properties on the `Schema`.
  These properties can be set in the constructor of the `Schema` instance, or within your DI composition root, or at any time before
  any query is executed. Once a query has been executed, changes to these fields is not allowed, and adding middleware via the field middleware
  builder has no effect.
* The signature of `IFieldMiddlewareBuilder.Use` has been changed to remove the schema from delegate. Since the schema is now known, there is no
  need for it to be passed to the middleware builder.
* The middleware `Use<T>` extension method has been removed. Please use the `Use` method with a middleware instance instead.
* `GraphQL.Utilities.ServiceProviderExtensions` has been made internal. This affects usages of its extension method `GetRequiredService`. Instead, reference the `Microsoft.Extensions.DependencyInjection.Abstractions` NuGet package and use extension method from `Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions` class.
* `GraphQL.Instrumentation.StatsReport` and its associated classes have been removed. Please copy the source code into your project if you require these classes.
* When used, Apollo tracing will now convert the starting timestamp to UTC so that `StartTime` and `EndTime` are properly serialized as UTC values.
* `ApolloTracing.ConvertTime` is now private and `ResolverTrace.Path` does not initialize an empty list when created.
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
* `Connection<TNode, TEdge>.TotalCount` has been changed from an `int` to an `int?`. This allows for returning `null` indicating that the total count is unknown.
* By default fields returned by introspection query are no longer sorted by their names. `LegacyV3SchemaComparer` can be used to switch to the old behavior.

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

* `INode.IsEqualTo` and related methods have been removed.
* `void Visit<TState>(System.Action<INode, TState> action, TState state)` method has been added.
* `SourceLocation`, `NameNode` and `BasicVisitor` have been changed to a `readonly struct`.
* `ErrorLocation` struct became `readonly`.
* `DebugNodeVisitor` class has been removed.
* Most methods and classes within `OverlappingFieldsCanBeMerged` are now private.
* `EnumerableExtensions.Apply` for dictionaries has been removed.
* `ObjectExtensions.GetInterface` has been removed along with two overloads of `GetPropertyValue`.
* Subscriptions implementation (`SubscriptionExecutionStrategy`) has been moved into `GraphQL.SystemReactive` project and default document executer throws `NotSupportedException`.
* `ISubscriptionExecuter` has been removed.
* `EnterLeaveListener` has been removed and the signatures of `INodeVisitor.Enter` and `INodeVisitor.Leave` have changed. `NodeVisitors` class has been added in its place.
* `TypeInfo.GetAncestors()` has been changed to `TypeInfo.GetAncestor(int index)`
* Various methods within `StringUtils` have been removed; please use extension methods within `StringExtensions` instead.
* `InputObjectGraphType.ParseDictionary` has been added so that customized deserialization behavior can be specified for input objects.
  If `InputObjectGraphType<T>` is used, and `GetArgument<T>` is called with the same type, no behavior changes will occur by default.
  If `InputObjectGraphType<T>` is used, but `GetArgument<T>` is called with a different type, coercion may fail. Override `ParseDictionary`
  to force resolving the input object to the correct type.
* `IResolveFieldContext.Arguments` now returns an `IDictionary<string, ArgumentValue>` instead of `IDictionary<string, object>` so that it
  can be determined if the value returned is a default value or if it is a specified literal or variable.
* `IResolveFieldContext.HasArgument` now returns `false` when `GetArgument` returns a field default value. Note that if a variable is specified,
  and the variable resolves to its default value, then `HasArgument` returns `true` (since the field argument is successfully resolving to a variable).
* Various `IEnumerable<T>` properties on schema and graph types have been changed to custom collections: `SchemaDirectives`, `SchemaTypes`, `TypeFields`, `PossibleTypes`, `ResolvedInterfaces`
* `GraphTypesLookup` has been renamed to `SchemaTypes` with a significant decrease in public APIs 
* `ExecutionHelper.GetVariableValue` has been removed, and the signature for `ExecutionHelper.CoerceValue` has changed.
* Removed `TypeExtensions.As`
* The `IResolveFieldContext` instance passed to field resolvers is re-used at the completion of the resolver. Be sure not to
  use this instance once the resolver finishes executing. To preserve a copy of the context, call `.Copy()` on the context
  to create a copy that is not re-used. Note that it is safe to use the field context within asynchronous field resolvers and
  data loaders. Once the asynchronous field resolver or data loader returns its final result, the context may be re-used.
  Also, any calls to the configured `UnhandledExceptionDelegate` will receive a field context copy that will not be re-used,
  so it is safe to preserve these instances without calling `.Copy()`.
* `ExecutionHelper.CollectFields` method was moved into `Fields` class and renamed to `CollectFrom`
* `IProvideMetadata.Metadata` is now `Dictionary` instead of `ConcurrentDictionary`, and is not thread safe anymore
* By default, descriptions for fields, types, enums, and so on are not pulled from xml comments unless the corresponding global flag is enabled.
* `ISchema.FindDirective`, `ISchema.RegisterDirective`, `ISchema.RegisterDirectives` methods were moved into `SchemaDirectives` class
* `ISchema.FindType` method was moved into `SchemaTypes[string typeName]` indexer
* Some of the `ISchemaNodeVisitor` methods have been changes to better support schema traversal
* Most `ExecutionStrategy` methods are now `protected`
* `ObjectExecutionNode.SubFields` property type was changed from `Dictionary<string, ExecutionNode>` to `ExecutionNode[]`
* `ExecutionNode.IsResultSet` has been removed
* `ExecutionNode.Source` is read-only; additional derived classes have been added for subscriptions
* `NameValidator.ValidateName` and `NameValidator.ValidateNameOnSchemaInitialize` accept an enum instead of a string for their second argument
