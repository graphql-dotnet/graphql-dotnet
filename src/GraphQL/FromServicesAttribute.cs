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
        public override void Modify<TReturnType>(ArgumentInformation<TReturnType> argumentInformation)
            => argumentInformation.Expression = context => GetRequiredService<TReturnType>(context);

        private static TServiceType GetRequiredService<TServiceType>(IResolveFieldContext context)
            => (context.RequestServices ?? throw new MissingRequestServicesException())
                .GetRequiredService<TServiceType>();
    }
}
