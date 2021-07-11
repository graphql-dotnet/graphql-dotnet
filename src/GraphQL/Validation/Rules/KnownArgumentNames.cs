#nullable enable

using System;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation.Errors;

namespace GraphQL.Validation.Rules
{
    /// <summary>
    /// Known argument names:
    ///
    /// A GraphQL field is only valid if all supplied arguments are defined by
    /// that field.
    /// </summary>
    public class KnownArgumentNames : IValidationRule
    {
        /// <summary>
        /// Returns a static instance of this validation rule.
        /// </summary>
        public static readonly KnownArgumentNames Instance = new KnownArgumentNames();

        /// <inheritdoc/>
        /// <exception cref="KnownArgumentNamesError"/>
        public Task<INodeVisitor> ValidateAsync(ValidationContext context) => _nodeVisitor;

        private static readonly Task<INodeVisitor> _nodeVisitor = new MatchingNodeVisitor<Argument>((node, context) =>
        {
            var argumentOf = context.TypeInfo.GetAncestor(2);
            if (argumentOf is Field)
            {
                var fieldDef = context.TypeInfo.GetFieldDef();
                if (fieldDef != null)
                {
                    var fieldArgDef = fieldDef.Arguments?.Find(node.Name);
                    if (fieldArgDef == null)
                    {
                        var parentType = context.TypeInfo.GetParentType() ?? throw new InvalidOperationException("Parent type must not be null.");
                        context.ReportError(new KnownArgumentNamesError(context, node, fieldDef, parentType));
                    }
                }
            }
            else if (argumentOf is Directive)
            {
                var directive = context.TypeInfo.GetDirective();
                if (directive != null)
                {
                    var directiveArgDef = directive.Arguments?.Find(node.Name);
                    if (directiveArgDef == null)
                    {
                        context.ReportError(new KnownArgumentNamesError(context, node, directive));
                    }
                }
            }
        }).ToTask();
    }
}
