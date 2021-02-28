using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Utilities.Visitors
{
    /// <summary>
    /// Visitor for unit tests. Adds descriptions to schema elements.
    /// </summary>
    public class DescriptionDirectiveVisitor : BaseSchemaNodeVisitor
    {
        private static void SetDescription<T>(T element) where T: IProvideMetadata, IProvideDescription
        {
            // if a value has already been set, prefer that
            if (element.Description == null)
            {
                var descr = element.FindAppliedDirective("description");
                if (descr != null && descr.FindArgument("text")?.Value is string str)
                {
                    element.Description = str;
                }
            }
        }

        public override void VisitSchema(ISchema schema) => SetDescription(schema);

        public override void VisitDirective(DirectiveGraphType directive, ISchema schema) => SetDescription(directive);

        public override void VisitScalar(ScalarGraphType scalar, ISchema schema) => SetDescription(scalar);

        public override void VisitObject(IObjectGraphType type, ISchema schema) => SetDescription(type);

        public override void VisitInputObject(IInputObjectGraphType type, ISchema schema) => SetDescription(type);

        public override void VisitFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema) => SetDescription(field);

        public override void VisitInputFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema) => SetDescription(field);

        public override void VisitFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema) => SetDescription(argument);

        public override void VisitDirectiveArgumentDefinition(QueryArgument argument, DirectiveGraphType directive, ISchema schema) => SetDescription(argument);

        public override void VisitInterface(IInterfaceGraphType iface, ISchema schema) => SetDescription(iface);

        public override void VisitUnion(UnionGraphType union, ISchema schema) => SetDescription(union);

        public override void VisitEnum(EnumerationGraphType type, ISchema schema) => SetDescription(type);

        public override void VisitEnumValue(EnumValueDefinition value, EnumerationGraphType type, ISchema schema) => SetDescription(value);
    }
}
