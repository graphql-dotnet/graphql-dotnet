using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Sets <see cref="FieldType.DefaultArgumentValues"/> for each <see cref="FieldType"/>.
    /// </summary>
    public sealed class FieldTypeDefaultArgumentsVisitor : BaseSchemaNodeVisitor
    {
        /// <summary>
        /// Returns a static instance of the <see cref="FieldTypeDefaultArgumentsVisitor"/> class.
        /// </summary>
        public static readonly FieldTypeDefaultArgumentsVisitor Instance = new();

        private FieldTypeDefaultArgumentsVisitor()
        {
        }

        /// <inheritdoc/>
        public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
        {
            if (field.Arguments?.Count > 0)
            {
                field.DefaultArgumentValues = field.Arguments.ToDictionary(arg => arg.Name, arg => new ArgumentValue(arg.DefaultValue, ArgumentSource.FieldDefault));
            }
        }

        /// <inheritdoc/>
        public override void VisitInterfaceFieldDefinition(FieldType field, IInterfaceGraphType type, ISchema schema)
        {
            if (field.Arguments?.Count > 0)
            {
                field.DefaultArgumentValues = field.Arguments.ToDictionary(arg => arg.Name, arg => new ArgumentValue(arg.DefaultValue, ArgumentSource.FieldDefault));
            }
        }
    }
}
