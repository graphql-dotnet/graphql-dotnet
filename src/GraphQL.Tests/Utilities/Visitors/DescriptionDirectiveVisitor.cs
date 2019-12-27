using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Utilities.Visitors
{
    /// <summary>
    /// Visitor for unit tests. Add description to schema elements.
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
            base.VisitObject(type);
            type.Description = GetArgument("description", string.Empty);
        }

        public override void VisitEnum(EnumerationGraphType type)
        {
            base.VisitEnum(type);
            type.Description = GetArgument("description", string.Empty);
        }

        public override void VisitEnumValue(EnumValueDefinition value)
        {
            base.VisitEnumValue(value);
            value.Description = GetArgument("description", string.Empty);
        }

        public override void VisitScalar(ScalarGraphType scalar)
        {
            base.VisitScalar(scalar);
            scalar.Description = GetArgument("description", string.Empty);
        }

        public override void VisitFieldDefinition(FieldType field)
        {
            base.VisitFieldDefinition(field);
            field.Description = GetArgument("description", string.Empty);
        }

        public override void VisitInterface(InterfaceGraphType interfaceDefinition)
        {
            base.VisitInterface(interfaceDefinition);
            interfaceDefinition.Description = GetArgument("description", string.Empty);
        }

        public override void VisitUnion(UnionGraphType union)
        {
            base.VisitUnion(union);
            union.Description = GetArgument("description", string.Empty);
        }

        public override void VisitArgumentDefinition(QueryArgument argument)
        {
            base.VisitArgumentDefinition(argument);
            argument.Description = GetArgument("description", string.Empty);
        }

        public override void VisitInputObject(InputObjectGraphType type)
        {
            base.VisitInputObject(type);
            type.Description = GetArgument("description", string.Empty);
        }

        public override void VisitInputFieldDefinition(FieldType value)
        {
            base.VisitInputFieldDefinition(value);
            value.Description = GetArgument("description", string.Empty);
        }
    }
}
