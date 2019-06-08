using GraphQL.Types;

namespace GraphQL.Utilities
{
    public class DeprecatedDirectiveVisistor : SchemaDirectiveVisitor
    {
        protected static readonly string DeprecatedDefaultValue = DirectiveGraphType.Deprecated.Arguments.Find("reason").DefaultValue.ToString();

        public override void VisitField(FieldType field)
        {
            // if a value has already been set, prefer that
            if (!string.IsNullOrWhiteSpace(field.DeprecationReason)) return;
            field.DeprecationReason = GetArgument<string>("reason") ?? DeprecatedDefaultValue;
        }

        public override void VisitEnumValue(EnumValueDefinition value)
        {
            // if a value has already been set, prefer that
            if (!string.IsNullOrWhiteSpace(value.DeprecationReason)) return;
            value.DeprecationReason = GetArgument<string>("reason") ?? DeprecatedDefaultValue;
        }
    }
}
