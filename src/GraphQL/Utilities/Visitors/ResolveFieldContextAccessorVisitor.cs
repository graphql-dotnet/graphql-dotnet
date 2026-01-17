using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities;

/// <summary>
/// Wraps field resolvers with <see cref="ResolveFieldContextAccessorResolver"/> to populate
/// the <see cref="IResolveFieldContextAccessor"/> during field resolution.
/// </summary>
internal sealed class ResolveFieldContextAccessorVisitor : BaseSchemaNodeVisitor
{
    private readonly IResolveFieldContextAccessor _accessor;

    /// <summary>
    /// Initializes a new instance with the specified context accessor.
    /// </summary>
    /// <param name="accessor">The context accessor to populate during field resolution.</param>
    public ResolveFieldContextAccessorVisitor(IResolveFieldContextAccessor accessor)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    /// <inheritdoc/>
    public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
    {
        // only wrap fields that have a custom resolver, as default resolvers do not need the context accessor
        // also, do not wrap fields that explicitly indicate they do not need the context accessor
        if (field.Resolver != null &&
            (field.Resolver is not IRequiresResolveFieldContextAccessor requiresAccessor || requiresAccessor.RequiresResolveFieldContextAccessor))
        {
            field.Resolver = new ResolveFieldContextAccessorResolver(_accessor, field.Resolver);
        }

        // also wrap stream resolvers if present
        if (field.StreamResolver != null)
        {
            field.StreamResolver = new ResolveFieldContextAccessorStreamResolver(_accessor, field.StreamResolver);
        }
    }
}
