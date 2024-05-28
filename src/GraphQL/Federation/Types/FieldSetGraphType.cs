using GraphQL.Types;

namespace GraphQL.Federation.Types;

internal class FieldSetGraphType : StringGraphType
{
    public FieldSetGraphType()
    {
        Name = "federation__FieldSet";
    }
}
