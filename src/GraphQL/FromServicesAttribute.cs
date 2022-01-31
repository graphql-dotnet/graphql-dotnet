using System;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromServicesAttribute : GraphQLAttribute
    {
        /// <inheritdoc/>
        public override void Modify<TReturnType>(ArgumentInformation<TReturnType> argumentInformation)
        {
            argumentInformation.Expression = context => context.RequestServices!.GetRequiredService<TReturnType>();
        }
    }
}
