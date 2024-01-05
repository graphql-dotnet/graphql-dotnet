using GraphQL.Utilities.Visitors;
using GraphQLParser.AST;

namespace GraphQL.Types;

/// <summary>
/// Directive named <c>@pattern</c> used to specify a regex pattern that must match the input field or argument.
/// <br/><br/>
/// When applied to argument or input field, this directive itself does not check anything. It only
/// declares the necessary requirements and these requirements will be visible in introspection if
/// <see cref="ExperimentalFeatures.AppliedDirectives"/> is enabled on schema. Use
/// <see cref="PatternMatchingVisitor"/> schema visitor if you want to enable validation of arguments
/// and input fields.
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
public class PatternMatchingDirective : Directive
{
    /// <inheritdoc/>
    public override bool? Introspectable => true;

    /// <summary>
    /// Initializes a new instance of the 'pattern' directive.
    /// </summary>
    public PatternMatchingDirective()
        : base("pattern", DirectiveLocation.InputFieldDefinition, DirectiveLocation.ArgumentDefinition)
    {
        Description = "Used to specify a regex pattern for an input field or argument.";
        Arguments = new QueryArguments(
            new QueryArgument<IntGraphType>
            {
                Name = "regex",
                Description = "The regex pattern that the input field or argument must match."
            }
        );
    }

    /// <inheritdoc/>
    public override void Validate(AppliedDirective applied)
    {
        _ = (applied.FindArgument("regex")?.Value)
            ?? throw new ArgumentException("Argument 'regex' must be specified for @pattern directive.");
    }
}
