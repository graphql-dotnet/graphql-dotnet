using System.Text.RegularExpressions;
using GraphQL.Types;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// A schema visitor that adds validation for the <see cref="PatternMatchingDirective"/> directive.
/// Use as follows to enable validation of arguments and input fields:
/// <code>
/// services.AddGraphQL(b => b
///     .AddSchema&lt;MySchema&gt;()
///     .ConfigureSchema(s =>
///     {
///         s.Directives.Register(new <see cref="PatternMatchingDirective"/>());
///         s.RegisterVisitor(new <see cref="PatternMatchingVisitor"/>());
///     }));
///
/// field.Field(x => x.Email)
///     .ApplyDirective("pattern", "regex", @".+\@.+\..+");
/// </code>
/// </summary>
public class PatternMatchingVisitor : BaseSchemaNodeVisitor
{
    /// <inheritdoc/>
    public override void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema)
        => field.Validator += GetValidator(null, field, type);

    /// <inheritdoc/>
    public override void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema)
        => argument.Validator += GetValidator(argument, field, type);

    /// <inheritdoc/>
    public override void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema)
        => field.Validator += GetValidator(argument, field, type);

    private static Action<object?>? GetValidator(QueryArgument? argument, FieldType field, INamedType type)
    {
        var fieldArgOrInputField = (argument as IMetadataReader) ?? field;
        // look for @pattern directive applied to the field argument or input field, and if found, use Validate to set the validation method
        var applied = fieldArgOrInputField.FindAppliedDirective("pattern");
        var regexArgument = applied?.FindArgument("regex");
        if (regexArgument == null)
            return null;

        if (regexArgument.Value == null)
            throw new ArgumentException($"Pattern directive 'regex' argument at {GetLocation()} must have non-null value.");

        if (regexArgument.Value is not string regex)
            throw new ArgumentException($"Pattern directive 'regex' argument at {GetLocation()} must be of 'string' type.");

        // if GlobalSwitches.DynamicallyCompileToObject is true, then compile
        // the regex (or pull from the static cache if already compiled)
        // (note that GlobalSwitches.DynamicallyCompileToObject is false for AOT schemas and scoped schemas)
        var regexObject = new Regex($"^{regex}$",
            GlobalSwitches.DynamicallyCompileToObject ? RegexOptions.Compiled : RegexOptions.None);

        return (value) =>
        {
            if (value is string stringValue && !regexObject.IsMatch(stringValue))
            {
                throw new ArgumentException($"Value '{stringValue}' does not match the regex pattern '{regex}'.");
            }
        };

        string GetLocation()
            => argument != null
                ? $"{type.Name}.{field.Name}.{argument.Name}"
                : $"{type.Name}.{field.Name}";
    }
}
