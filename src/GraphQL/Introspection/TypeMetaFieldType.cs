using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class TypeMetaFieldType : FieldType
    {
        public TypeMetaFieldType()
        {
            Name = "__type";
            Type = new __Type();
            Description = "Request the type information of a single type.";
            Arguments = new QueryArguments(new[]
            {
                new QueryArgument
                {
                    Name = "name",
                    Type = NonNullGraphType.String
                }
            });
            Resolve = context => context.Schema.FindType((string) context.Arguments["name"]);
        }
    }
}