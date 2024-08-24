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
        => field.Validator += GetValidator(field);

    /// <inheritdoc/>
    public override void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema)
        => argument.Validator += GetValidator(argument);

    /// <inheritdoc/>
    public override void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema)
        => field.Validator += GetValidator(argument);

    private static Action<object?>? GetValidator(IMetadataReader fieldArgOrInputField)
    {
        // look for @pattern directive applied to the field argument or input field, and if found, use Validate to set the validation method
        var applied = fieldArgOrInputField.FindAppliedDirective("pattern");
        var value = applied?.FindArgument("regex")?.Value;

        var regexObject = value switch
        {
            // if GlobalSwitches.DynamicallyCompileToObject is true, then compile
            // the regex (or pull from the static cache if already compiled)
            // (note that GlobalSwitches.DynamicallyCompileToObject is false for AOT schemas and scoped schemas)
            string regex => new Regex($"^{regex}$",
                GlobalSwitches.DynamicallyCompileToObject ? RegexOptions.Compiled : RegexOptions.None),
            Regex r => r,
            _ => null
        };
        if (regexObject == null)
            return null;
        var regexString = value switch
        {
            string regex => regex,
            Regex r => StripChars(r),
            _ => throw new InvalidOperationException()
        };

        return (value) =>
        {
            if (value is string stringValue && !regexObject.IsMatch(stringValue))
            {
                throw new ArgumentException($"Value '{stringValue}' does not match the regex pattern '{regexString}'.");
            }
        };

        string StripChars(Regex r)
        {
            var s = r.ToString();
            if (s.StartsWith("^") && s.EndsWith("$"))
            {
                s = s.Substring(1);
                s = s.Substring(0, s.Length - 1);
            }
            else
                throw new InvalidOperationException("Regex must start with ^ and end with $");
            return s;
        }
    }
}
