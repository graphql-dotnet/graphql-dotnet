using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Specifies that the method argument should be pulled from <see cref="IResolveFieldContext.Source"/>,
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromUserContextAttribute : GraphQLAttribute
    {
        /// <inheritdoc/>
        public override void Modify<TParameterType>(ArgumentInformation argumentInformation)
        {
            argumentInformation.SetDelegate(context => (TParameterType)context.UserContext!);
        }
    }
}
