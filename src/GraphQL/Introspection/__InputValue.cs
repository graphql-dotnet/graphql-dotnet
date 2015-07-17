using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __InputValue : ObjectGraphType
    {
        public __InputValue()
        {
            Name = "__InputValue";
            Field("name", NonNullGraphType.String);
            Field("description", ScalarGraphType.String);
            Field("type", new NonNullGraphType(new __Type()));
            Field("defaultValue", ScalarGraphType.String, null, context =>
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