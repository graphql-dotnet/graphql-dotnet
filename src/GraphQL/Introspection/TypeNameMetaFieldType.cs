using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__typename</c> meta-field is available on every type and
    /// returns the name of the type on which it was requested.
    /// </summary>
    public class TypeNameMetaFieldType : FieldType
    {
        /// <summary>
        /// Initializes a new instance of the <c>__typename</c> meta-field.
        /// </summary>
        public TypeNameMetaFieldType()
        {
            SetName("__typename", validate: false);
            Type = typeof(NonNullGraphType<StringGraphType>);
            Description = "The name of the current Object type at runtime.";
            Resolver = new FuncFieldResolver<object>(context => context.ParentType.Name);
        }
    }
}
