using GraphQL.MicrosoftDI;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Creates a dedicated service scope during the field resolver's execution.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ScopedAttribute : GraphQLAttribute
    {
        /// <inheritdoc/>
        public override void Modify(FieldType fieldType, bool isInputType)
        {
            if (isInputType)
                return;

            if (fieldType.Resolver != null)
            {
                fieldType.Resolver = new DynamicScopedFieldResolver(fieldType.Resolver);
            }

            if (fieldType.Subscriber != null)
            {
                fieldType.Subscriber = new DynamicScopedSourceStreamResolver(fieldType.Subscriber);
            }
        }
    }
}
