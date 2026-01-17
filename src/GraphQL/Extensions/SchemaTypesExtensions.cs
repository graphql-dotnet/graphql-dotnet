using GraphQL.Instrumentation;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Extension methods for <see cref="SchemaTypesBase"/>.
/// </summary>
public static class SchemaTypesExtensions
{
    /// <summary>
    /// Applies all delegates specified by the middleware builder to the schema types.
    /// <br/><br/>
    /// When applying to the schema, modifies the resolver of each field of each graph type adding required behavior.
    /// Therefore, as a rule, this method should be called only once - during schema initialization.
    /// </summary>
    /// <param name="schemaTypes">The schema types collection to apply middleware to.</param>
    /// <param name="fieldMiddlewareBuilder">The middleware builder containing the middleware to apply.</param>
    /// <param name="schema">The schema instance to retrieve the default field resolver from.</param>
    public static void ApplyMiddleware(this SchemaTypesBase schemaTypes, IFieldMiddlewareBuilder fieldMiddlewareBuilder, ISchema schema)
    {
        var transform = (fieldMiddlewareBuilder ?? throw new ArgumentNullException(nameof(fieldMiddlewareBuilder))).Build();

        // allocation free optimization if no middlewares are defined
        if (transform != null)
        {
            schemaTypes.ApplyMiddleware(transform, schema);
        }
    }

    /// <summary>
    /// Applies the specified middleware transform delegate to the schema types.
    /// <br/><br/>
    /// When applying to the schema, modifies the resolver of each field of each graph type adding required behavior.
    /// Therefore, as a rule, this method should be called only once - during schema initialization.
    /// </summary>
    /// <param name="schemaTypes">The schema types collection to apply middleware to.</param>
    /// <param name="transform">The middleware transform delegate to apply.</param>
    /// <param name="schema">The schema instance to retrieve the default field resolver from.</param>
    public static void ApplyMiddleware(this SchemaTypesBase schemaTypes, Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> transform, ISchema schema)
    {
        if (schemaTypes == null)
            throw new ArgumentNullException(nameof(schemaTypes));
        if (transform == null)
            throw new ArgumentNullException(nameof(transform));
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));

        foreach (var graphType in schemaTypes.Dictionary.Values)
        {
            if (graphType is IObjectGraphType obj)
            {
                foreach (var field in obj.Fields.List)
                {
                    var inner = field.Resolver ?? (field.StreamResolver == null
                        ? (schema.DefaultFieldResolver ?? throw new InvalidOperationException($"Schema.DefaultFieldResolver is null for field '{field.Name}' on type '{obj.Name}'."))
                        : SourceFieldResolver.Instance);

                    var fieldMiddlewareDelegate = transform(inner.ResolveAsync);

                    field.Resolver = new FuncFieldResolver<object>(fieldMiddlewareDelegate.Invoke);
                }
            }
        }
    }
}
