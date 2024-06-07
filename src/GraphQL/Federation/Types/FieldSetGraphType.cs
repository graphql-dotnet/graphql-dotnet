using GraphQL.Types;

namespace GraphQL.Federation.Types;

public class FieldSetGraphType : StringGraphType
{
    public FieldSetGraphType()
    {
        Name = "federation__FieldSet";
    }
}
