using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class TypeMetaFieldType : FieldType
    {
        public TypeMetaFieldType()
        {
            Name = "__type";
            Type = typeof(__Type);
            Description = "Request the type information of a single type.";
            Arguments = new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>>
                {
                    Name = "name"
                });
            Resolver = new FuncFieldResolver<object>(context => context.Schema.FindType((string) context.Arguments["name"]));
        }
    }
}
