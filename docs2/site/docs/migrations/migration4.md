# Migrating from v3.x to v4.x

## New Features

* Extension methods to configure authorization requirements for GraphQL elements: types, fields, schema.
* New property `GraphQL.Introspection.ISchemaComparer ISchema.Comparer { get; set; }`
* New property `IResolveFieldContext.ArrayPool`
* New method `IParentExecutionNode.ApplyToChildren`
* Document caching supported via `IDocumentCache` and a default implementation within `DefaultDocumentCache`.
  Within the `GraphQL.Caching` nuget package, a memory-backed implementation is available which is backed by `Microsoft.Extensions.Caching.Memory.IMemoryCache`.

## Breaking Changes

* `NameConverter` and `SchemaFilter` have been removed from `ExecutionOptions` and are now properties on the `Schema`.
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
