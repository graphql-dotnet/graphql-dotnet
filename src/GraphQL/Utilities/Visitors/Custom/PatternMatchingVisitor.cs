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
    {
        // look for @pattern directive applied to the field, and if found, use Validate to set the validation method
        var applied = field.FindAppliedDirective("pattern");
        if (applied?.FindArgument("regex")?.Value is not string regex)
            return;

        // if GlobalSwitches.DynamicallyCompileToObject is true, then compile
        // the regex (or pull from the static cache if already compiled)
        // (note that GlobalSwitches.DynamicallyCompileToObject is false for AOT schemas and scoped schemas)
        var regexObject = new Regex($"^{regex}$",
            GlobalSwitches.DynamicallyCompileToObject ? RegexOptions.Compiled : RegexOptions.None);

        // set the validation method
        field.Validator += (value) =>
        {
            if (value is string stringValue && !regexObject.IsMatch(stringValue))
            {
                throw new ArgumentException($"Value '{stringValue}' does not match the regex pattern '{regex}'.");
            }
        };
    }

    /// <inheritdoc/>
    public override void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema)
    {
        // look for @pattern directive applied to the field, and if found, use Validate to set the validation method
        var applied = argument.FindAppliedDirective("pattern");
        if (applied?.FindArgument("regex")?.Value is not string regex)
            return;

        // compile the regex
        var regexObject = new Regex(regex,
            GlobalSwitches.DynamicallyCompileToObject ? RegexOptions.Compiled : RegexOptions.None);

        // set the validation method
        argument.Validator += (value) =>
        {
            if (value is string stringValue && !regexObject.IsMatch(stringValue))
            {
                throw new ArgumentException($"Value '{stringValue}' does not match the regex pattern '{regex}'.");
            }
        };
    }
}
