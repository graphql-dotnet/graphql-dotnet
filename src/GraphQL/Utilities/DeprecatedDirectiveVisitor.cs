using GraphQL.Types;

namespace GraphQL.Utilities
{
    public class DeprecatedDirectiveVisitor : SchemaDirectiveVisitor
    {
        public override string Name => "deprecated";

        protected static readonly string DeprecatedDefaultValue = DirectiveGraphType.Deprecated.Arguments.Find("reason").DefaultValue.ToString();

        public override void VisitFieldDefinition(FieldType field)
        {
            base.VisitFieldDefinition(field);

            // if a value has already been set, prefer that
            if (!string.IsNullOrWhiteSpace(field.DeprecationReason))
                return;
            field.DeprecationReason = GetArgument<string>("reason") ?? DeprecatedDefaultValue;
        }

        public override void VisitEnumValue(EnumValueDefinition value)
        {
            base.VisitEnumValue(value);

            // if a value has already been set, prefer that
            if (!string.IsNullOrWhiteSpace(value.DeprecationReason))
                return;
            value.DeprecationReason = GetArgument<string>("reason") ?? DeprecatedDefaultValue;
        }
    }
}
