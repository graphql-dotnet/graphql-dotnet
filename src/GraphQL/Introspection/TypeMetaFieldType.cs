using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__type</c> meta-field is available on the root of a query operation and
    /// returns a <c>__Type</c> graph type for a specified graph type name.
    /// </summary>
    public class TypeMetaFieldType : FieldType
    {
        /// <summary>
        /// Initializes a new instance of the <c>__type</c> meta-field.
        /// </summary>
        public TypeMetaFieldType()
        {
            SetName("__type", validate: false);
            Type = typeof(__Type);
            Description = "Request the type information of a single type.";
            Arguments = Arg.Next("name").String().NonNull();
            Resolver = new FuncFieldResolver<object>(context => context.Schema.FindType(context.GetArgument<string>("name")));
        }
    }
}
