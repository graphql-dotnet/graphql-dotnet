# Migrating from v3.x to v4.x

## New Features

* Extension methods to configure authorization requirements for GraphQL elements: types, fields, schema.
* New property `GraphQL.Introspection.ISchemaComparer ISchema.Comparer { get; set; }`

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
* `ErrorLocation` struct became `readonly`.
* `SourceLocation` class became `readonly struct`.
* `CoreToVanillaConverter` class became `static` and most of its members have been removed.
* `GraphQL.Language.AST.Field.MergeSelectionSet` method has been removed.
* `CoreToVanillaConverter.Convert` method now requires only one `GraphQLDocument` argument.
* `TypeCollectionContext` class is now internal, also all methods with this parameter in `GraphTypesLookup` are private.
* `GraphQLTypeReference` class is now internal, also `GraphTypesLookup.ApplyTypeReferences` is now private.
* `IHaveDefaultValue.Type` has been moved to `IProvideResolvedType.Type`
* `Connection<TNode, TEdge>.TotalCount` has been changed from an `int` to an `int?`. This allows for returning `null` indicating that the total count is unknown.
* By default fields returned by introspection query are no longer sorted by their names. `LegacyV3SchemaComparer` can be used to switch to the old behavior.
* `INode.IsEqualTo` and related methods have been removed.
* `NameNode` has been changed to a readonly struct.
* `DebugNodeVisitor` class has been removed.
* Most methods and classes within `OverlappingFieldsCanBeMerged` are now private.
* `EnumerableExtensions.Apply` for dictionaries has been removed.
* `ObjectExtensions.GetInterface` has been removed along with two overloads of `GetPropertyValue`.

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

* Subscriptions implementation (`SubscriptionExecutionStrategy`) has been moved into `GraphQL.SystemReactive` project and default document executer throws `NotSupportedException`.
* `ISubscriptionExecuter` has been removed.
