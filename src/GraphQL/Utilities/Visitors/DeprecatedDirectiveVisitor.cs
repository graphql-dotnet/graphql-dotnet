using GraphQL.Types;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Visitor that sets <see cref="IProvideDeprecationReason.DeprecationReason"/> property to the corresponding schema elements.
    /// </summary>
    public class DeprecatedDirectiveVisitor : BaseSchemaNodeVisitor
    {
        protected static readonly string DeprecatedDefaultValue = DirectiveGraphType.Deprecated.Arguments.Find("reason").DefaultValue.ToString();

        /// <summary>
        /// Returns a static instance of the <see cref="DeprecatedDirectiveVisitor"/>.
        /// </summary>
        public static DeprecatedDirectiveVisitor Instance { get; } = new DeprecatedDirectiveVisitor();

        private static void SetDeprecationReason<T>(T element) where T : IProvideMetadata, IProvideDeprecationReason
        {
            // if a value has already been set, prefer that
            if (element.DeprecationReason == null && element.HasAppliedDirectives())
            {
                var deprecated = element.GetAppliedDirectives().Find("deprecated");
                if (deprecated != null)
                {
                    element.DeprecationReason = deprecated.FindArgument("reason")?.Value is string str
                        ? str
                        : DeprecatedDefaultValue;
                }
            }
        }

        /// <inheritdoc />
        public override void VisitFieldDefinition(FieldType field, ISchema schema) => SetDeprecationReason(field);

        /// <inheritdoc />
        public override void VisitEnumValue(EnumValueDefinition value, ISchema schema) => SetDeprecationReason(value);
    }
}
