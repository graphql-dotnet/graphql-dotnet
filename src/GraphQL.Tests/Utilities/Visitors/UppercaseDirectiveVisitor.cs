using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;

namespace GraphQL.Tests.Utilities.Visitors;

public class UpperDirective : Directive
{
    public UpperDirective()
        : base("upper", DirectiveLocation.FieldDefinition)
    {
        Description = "Converts the value of string fields to uppercase.";
    }
}

/// <summary>
/// Visitor for unit tests. Wraps field resolver and returns UPPERCASED result if it is string.
/// </summary>
public class UppercaseDirectiveVisitor : BaseSchemaNodeVisitor
{
    public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
    {
        var applied = field.FindAppliedDirective("upper");
        if (applied != null)
        {
            var inner = field.Resolver ?? NameFieldResolver.Instance;
            field.Resolver = new FuncFieldResolver<object>(async context =>
            {
                object result = await inner.ResolveAsync(context).ConfigureAwait(false);

                return result is string str ? str.ToUpperInvariant() : result;
            });
        }
    }
}
