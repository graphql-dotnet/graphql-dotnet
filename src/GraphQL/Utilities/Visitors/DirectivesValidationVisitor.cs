using System;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    internal sealed class DirectivesValidationVisitor : BaseSchemaNodeVisitor
    {
        internal static readonly DirectivesValidationVisitor Instance = new DirectivesValidationVisitor();

        public override void VisitDirective(DirectiveGraphType directive, Schema schema)
        {
             if (directive.Locations.Count == 0)
                throw new InvalidOperationException($"Directive '{directive}' must have locations");
        }
    }
}
