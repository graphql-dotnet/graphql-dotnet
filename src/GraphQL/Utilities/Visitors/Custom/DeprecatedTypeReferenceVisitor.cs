using GraphQL.Types;

namespace GraphQL.Utilities.Visitors.Custom;

/// <summary>
/// A schema visitor that identifies non-deprecated fields that directly reference deprecated types.
/// This visitor helps maintain schema consistency by flagging potential issues where deprecated
/// types are still being referenced by active fields.
/// </summary>
/// <remarks>
/// Use this visitor to scan your schema for fields that may need attention when deprecating types.
/// The visitor will throw exceptions during schema initialization if violations are found.
///
/// Example usage:
/// <code>
/// schema.RegisterVisitor(new DeprecatedTypeReferenceVisitor());
/// schema.Initialize(); // Will throw if violations are found
/// </code>
/// </remarks>
public class DeprecatedTypeReferenceVisitor : BaseSchemaNodeVisitor
{
    /// <summary>
    /// Internal list of violations found during schema traversal.
    /// Each violation represents a non-deprecated field that references a deprecated type.
    /// </summary>
    private readonly List<string> _violations = new List<string>();

    /// <inheritdoc/>
    public override void PostVisitSchema(ISchema schema)
    {
        if (_violations.Count == 1)
        {
            throw new InvalidOperationException(_violations[0]);
        }
        else if (_violations.Count > 1)
        {
            var exceptions = _violations.Select(violation => new InvalidOperationException(violation)).ToArray();
            throw new AggregateException("Schema validation failed. Found multiple non-deprecated fields referencing deprecated types.", exceptions);
        }
    }

    /// <inheritdoc/>
    public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
    {
        CheckFieldForDeprecatedTypeReference(field, type.Name);
    }

    /// <inheritdoc/>
    public override void VisitInterfaceFieldDefinition(FieldType field, IInterfaceGraphType type, ISchema schema)
    {
        CheckFieldForDeprecatedTypeReference(field, type.Name);
    }

    /// <inheritdoc/>
    public override void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema)
    {
        CheckFieldForDeprecatedTypeReference(field, type.Name);
    }

    /// <inheritdoc/>
    public override void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema)
    {
        CheckArgumentForDeprecatedTypeReference(argument, field, type.Name);
    }

    /// <inheritdoc/>
    public override void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema)
    {
        CheckArgumentForDeprecatedTypeReference(argument, field, type.Name);
    }

    private void CheckFieldForDeprecatedTypeReference(FieldType field, string parentTypeName)
    {
        // Skip if the field itself is deprecated
        if (IsDeprecated(field))
            return;

        var fieldType = field.ResolvedType?.GetNamedType();
        if (fieldType != null && IsDeprecated(fieldType))
        {
            var violation = $"Non-deprecated field '{parentTypeName}.{field.Name}' references deprecated type '{fieldType.Name}'.";
            _violations.Add(violation);
        }
    }

    private void CheckArgumentForDeprecatedTypeReference(QueryArgument argument, FieldType field, string parentTypeName)
    {
        // Skip if the argument itself is deprecated
        if (IsDeprecated(argument))
            return;

        // Skip if the parent field is deprecated
        if (IsDeprecated(field))
            return;

        var argumentType = argument.ResolvedType?.GetNamedType();
        if (argumentType != null && IsDeprecated(argumentType))
        {
            var violation = $"Non-deprecated argument '{parentTypeName}.{field.Name}.{argument.Name}' references deprecated type '{argumentType.Name}'.";
            _violations.Add(violation);
        }
    }

    /// <summary>
    /// Determines if a schema element is deprecated by checking its DeprecationReason property.
    /// </summary>
    /// <param name="element">The schema element to check.</param>
    /// <returns>True if the element is deprecated, false otherwise.</returns>
    private static bool IsDeprecated(IProvideDeprecationReason element)
    {
        return !string.IsNullOrEmpty(element.DeprecationReason);
    }
}
