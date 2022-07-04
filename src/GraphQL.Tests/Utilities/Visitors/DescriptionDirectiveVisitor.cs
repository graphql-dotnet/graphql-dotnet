using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Tests.Utilities.Visitors;

/// <summary>
/// Visitor for unit tests. Adds descriptions to schema elements.
/// </summary>
public class DescriptionDirectiveVisitor : BaseSchemaNodeVisitor
{
    private static void SetDescription<T>(T element) where T : IProvideMetadata, IProvideDescription
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

    public override void VisitDirective(Directive directive, ISchema schema) => SetDescription(directive);

    public override void VisitScalar(ScalarGraphType type, ISchema schema) => SetDescription(type);

    public override void VisitObject(IObjectGraphType type, ISchema schema) => SetDescription(type);

    public override void VisitInputObject(IInputObjectGraphType type, ISchema schema) => SetDescription(type);

    public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema) => SetDescription(field);

    public override void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema) => SetDescription(field);

    public override void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema) => SetDescription(argument);

    public override void VisitDirectiveArgumentDefinition(QueryArgument argument, Directive directive, ISchema schema) => SetDescription(argument);

    public override void VisitInterface(IInterfaceGraphType type, ISchema schema) => SetDescription(type);

    public override void VisitUnion(UnionGraphType type, ISchema schema) => SetDescription(type);

    public override void VisitEnum(EnumerationGraphType type, ISchema schema) => SetDescription(type);

    public override void VisitEnumValue(EnumValueDefinition value, EnumerationGraphType type, ISchema schema) => SetDescription(value);
}
