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
            if (element.Description == null && element.HasAppliedDirectives())
            {
                var descr = element.GetAppliedDirectives().Find("description");
                if (descr != null && descr.FindArgument("text")?.Value is string str)
                {
                    element.Description = str;
                }
            }
        }

        public override void VisitSchema(Schema schema) => SetDescription(schema);

        public override void VisitDirective(DirectiveGraphType directive) => SetDescription(directive);

        public override void VisitScalar(ScalarGraphType scalar) => SetDescription(scalar);

        public override void VisitObject(IObjectGraphType type) => SetDescription(type);

        public override void VisitInputObject(IInputObjectGraphType type) => SetDescription(type);

        public override void VisitFieldDefinition(FieldType field) => SetDescription(field);

        public override void VisitInputFieldDefinition(FieldType field) => SetDescription(field);

        public override void VisitFieldArgumentDefinition(QueryArgument argument) => SetDescription(argument);

        public override void VisitDirectiveArgumentDefinition(QueryArgument argument) => SetDescription(argument);

        public override void VisitInterface(IInterfaceGraphType iface) => SetDescription(iface);

        public override void VisitUnion(UnionGraphType union) => SetDescription(union);

        public override void VisitEnum(EnumerationGraphType type) => SetDescription(type);

        public override void VisitEnumValue(EnumValueDefinition value) => SetDescription(value);
    }
}
