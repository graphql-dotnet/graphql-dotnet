using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Specifies that the method argument should be pulled from <see cref="IResolveFieldContext.Source"/>,
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromSourceAttribute : GraphQLAttribute
    {
        /// <inheritdoc/>
        public override void Modify<TParameterType>(ArgumentInformation argumentInformation)
        {
            if (argumentInformation.SourceType != null && !argumentInformation.ParameterInfo.ParameterType.IsAssignableFrom(argumentInformation.SourceType))
                throw new InvalidOperationException($"Source parameter type '{argumentInformation.ParameterInfo.ParameterType.Name}' does not match source type of '{argumentInformation.SourceType.Name}'.");
            argumentInformation.SetDelegate(context => (TParameterType)context.Source!);
        }
    }
}
