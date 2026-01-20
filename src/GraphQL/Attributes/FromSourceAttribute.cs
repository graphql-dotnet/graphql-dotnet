using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Specifies that the method argument should be pulled from <see cref="IResolveFieldContext.Source"/>,
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromSourceAttribute : ParameterAttribute
{
    /// <inheritdoc/>
    public override Func<IResolveFieldContext, T> GetResolver<T>(ArgumentInformation argumentInformation)
    {
        if (argumentInformation.SourceType != null && !typeof(T).IsAssignableFrom(argumentInformation.SourceType))
            throw new InvalidOperationException($"Source parameter type '{typeof(T).Name}' does not match source type of '{argumentInformation.SourceType.Name}'.");
        return context => (T)context.Source!;
    }
}
