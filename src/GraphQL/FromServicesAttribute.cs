using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    /// <summary>
    /// Specifies that the method argument should be pulled from <see cref="IResolveFieldContext.RequestServices"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromServicesAttribute : GraphQLAttribute
    {
        /// <inheritdoc/>
        public override void Modify(ArgumentInformation argumentInformation)
            => argumentInformation.Expression = context => GetRequiredService(context, argumentInformation.ParameterInfo.ParameterType);

        private static object GetRequiredService(IResolveFieldContext context, Type serviceType)
            => (context.RequestServices ?? throw new MissingRequestServicesException())
                .GetRequiredService(serviceType);
    }
}
