using GraphQL.Execution;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Sets <see cref="ObjectFieldType.DefaultArgumentValues"/> for each <see cref="ObjectFieldType"/>.
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
        public override void VisitObjectFieldDefinition(ObjectFieldType field, IObjectGraphType type, ISchema schema)
        {
            if (field.Arguments?.Count > 0)
            {
                field.DefaultArgumentValues = field.Arguments.ToDictionary(arg => arg.Name, arg => new ArgumentValue(arg.DefaultValue, ArgumentSource.FieldDefault));
            }
        }
    }
}
