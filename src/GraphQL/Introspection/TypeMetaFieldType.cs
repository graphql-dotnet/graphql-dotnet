using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class TypeMetaFieldType : FieldType
    {
        public TypeMetaFieldType()
        {
            SetName("__type", validate: false);
            Type = typeof(__Type);
            Description = "Request the type information of a single type.";
            Arguments = new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>>
                {
                    Name = "name"
                });
            Resolver = new FuncFieldResolver<object>(context => context.Schema.FindType(context.GetArgument<string>("name")));
        }
    }
}
