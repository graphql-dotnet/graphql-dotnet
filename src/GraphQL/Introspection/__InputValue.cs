using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __InputValue : ObjectGraphType
    {
        public __InputValue()
        {
            Name = "__InputValue";
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<NonNullGraphType<__Type>>("type");
            Field<StringGraphType>("defaultValue", null, null, context =>
            {
                var hasDefault = context.Source as IHaveDefaultValue;
                if (hasDefault != null && hasDefault.DefaultValue != null)
                {
                    return hasDefault.DefaultValue.ToString();
                }
                return null;
            });
        }
    }
}
