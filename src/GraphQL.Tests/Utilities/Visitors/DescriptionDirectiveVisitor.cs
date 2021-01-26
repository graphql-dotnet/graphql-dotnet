using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Utilities.Visitors
{
    /// <summary>
    /// Visitor for unit tests. Adds descriptions to schema elements.
    /// </summary>
    public class DescriptionDirectiveVisitor : SchemaDirectiveVisitor
    {
        public override string Name => "description";

        public DescriptionDirectiveVisitor() { }

        public DescriptionDirectiveVisitor(string description)
        {
            Arguments.Add("description", description);
        }

        public override void VisitObject(IObjectGraphType type)
        {
            type.Description = GetArgument("description", string.Empty);
        }

        public override void VisitEnum(EnumerationGraphType type)
        {
            type.Description = GetArgument("description", string.Empty);
        }

        public override void VisitEnumValue(EnumValueDefinition value)
        {
            value.Description = GetArgument("description", string.Empty);
        }

        public override void VisitScalar(ScalarGraphType scalar)
        {
            scalar.Description = GetArgument("description", string.Empty);
        }

        public override void VisitFieldDefinition(FieldType field)
        {
            field.Description = GetArgument("description", string.Empty);
        }

        public override void VisitInterface(InterfaceGraphType interfaceDefinition)
        {
            interfaceDefinition.Description = GetArgument("description", string.Empty);
        }

        public override void VisitUnion(UnionGraphType union)
        {
            union.Description = GetArgument("description", string.Empty);
        }

        public override void VisitArgumentDefinition(QueryArgument argument)
        {
            argument.Description = GetArgument("description", string.Empty);
        }

        public override void VisitInputObject(InputObjectGraphType type)
        {
            type.Description = GetArgument("description", string.Empty);
        }

        public override void VisitInputFieldDefinition(FieldType value)
        {
            value.Description = GetArgument("description", string.Empty);
        }
    }
}
