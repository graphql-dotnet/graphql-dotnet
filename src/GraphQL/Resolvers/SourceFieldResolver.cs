namespace GraphQL.Resolvers;

/// <summary>
/// Returns value of <see cref="IResolveFieldContext.Source"/>.
/// </summary>
public sealed class SourceFieldResolver : IFieldResolver
{
    private SourceFieldResolver() { }

    /// <summary>
    /// Returns the static instance of the <see cref="SourceFieldResolver"/> class.
    /// </summary>
    public static SourceFieldResolver Instance { get; } = new();

    /// <inheritdoc/>
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context) => new(context.Source);
}
