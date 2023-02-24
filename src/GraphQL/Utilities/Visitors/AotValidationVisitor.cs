using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Utilities;

/// <summary>
/// Validates the schema meets limitations as required by the official specification. Also looks for
/// default values within arguments and inputs fields which are stored in AST nodes
/// and coerces them to their internally represented values.
/// </summary>
public class AotValidationVisitor : BaseSchemaNodeVisitor
{
    /// <summary>
    /// Returns a static instance of the <see cref="AotValidationVisitor"/> class.
    /// </summary>
    public static AotValidationVisitor Instance { get; } = new();

    /// <inheritdoc/>
    public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
    {
        if (type is IInputObjectGraphType)
        {
            // validate that an expression has been tied to the field
            if (field.GetMetadata<string?>(ComplexGraphType<string>.ORIGINAL_EXPRESSION_PROPERTY_NAME) == null)
            {
                throw new InvalidOperationException("All fields on input objects must be defined with an expression in scenarios when dynamic compilation is not supported by the runtime. " +
                    $"Please fix {type.Name}.{field.Name}.");
            }
        }
        else if (type is not IInterfaceGraphType)
        {
            if (field.Resolver == null || field.Resolver is NameFieldResolver)
            {
                throw new InvalidOperationException("NameFieldResolver is not supported in scenarios when dynamic compilation is not supported by the runtime. " +
                    $"Please fix {type.Name}.{field.Name}.");
            }
        }
    }
}
