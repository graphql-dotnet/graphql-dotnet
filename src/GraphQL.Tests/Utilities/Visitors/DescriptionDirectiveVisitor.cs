using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Utilities.Visitors
{
    /// <summary>
    /// Visitor for unit tests. Add description to schema elements.
    /// </summary>
    public class DescriptionDirectiveVisitor : SchemaDirectiveVisitor
    {
        public DescriptionDirectiveVisitor()
        {
            Name = "description";
        }

        public DescriptionDirectiveVisitor(string description)
            : this()
        {
            Arguments.Add("description", description);
        }

        public override void VisitObject(IObjectGraphType type)
        {
            base.VisitObject(type);
            type.Description = GetArgument("description", string.Empty);
        }

        public override void VisitObject(ObjectGraphType type)
        {
            base.VisitObject(type);
            type.Description = GetArgument("description", string.Empty);
        }

        public override void VisitEnumeration(EnumerationGraphType type)
        {
            base.VisitEnumeration(type);
            type.Description = GetArgument("description", string.Empty);
        }

        public override void VisitEnumerationValue(EnumValueDefinition value)
        {
            base.VisitEnumerationValue(value);
            value.Description = GetArgument("description", string.Empty);
        }

        public override void VisitScalar(ScalarGraphType scalar)
        {
            base.VisitScalar(scalar);
            scalar.Description = GetArgument("description", string.Empty);
        }

        public override void VisitField(FieldType field)
        {
            base.VisitField(field);
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

        public override void VisitArgument(QueryArgument argument)
        {
            base.VisitArgument(argument);
            argument.Description = GetArgument("description", string.Empty);
        }

        public override void VisitInputObject(InputObjectGraphType type)
        {
            base.VisitInputObject(type);
            type.Description = GetArgument("description", string.Empty);
        }

        public override void VisitInputField(FieldType value)
        {
            base.VisitInputField(value);
            value.Description = GetArgument("description", string.Empty);
        }
    }
}
