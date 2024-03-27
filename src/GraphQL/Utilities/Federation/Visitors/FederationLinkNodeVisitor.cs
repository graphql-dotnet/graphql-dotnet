#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using GraphQL.Utilities.Federation.Types;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using static GraphQL.Utilities.Federation.FederationHelper;
using GraphQLParser.AST;
using GraphQL.Utilities.Federation.Enums;

namespace GraphQL.Utilities.Federation.Visitors
{
    public class FederationLinkNodeVisitor : BaseSchemaNodeVisitor
    {
        public override void VisitSchema(ISchema schema)
        {
            FederationDirectiveEnum fedDirectives = getFedDirectives(schema);
            if (fedDirectives != FederationDirectiveEnum.None)
                schema.BuildLinkExtension(fedDirectives, "2.5");
        }

        private FederationDirectiveEnum getFedDirectives(ISchema schema)
        {
            FederationDirectiveEnum fedDirectives = FederationDirectiveEnum.None;
            var directives = schema.Directives;
            if (directives != null)
            {
                foreach (Directive directive in directives)
                {
                    string d = $"@{directive.Name}";
                    if (directive != null && FederationDirectiveEnumMap.ContainsValue(d))
                    {
                        var directiveEnum = FederationDirectiveEnumMap.FirstOrDefault(x => x.Value == d).Key;
                        if (!fedDirectives.HasFlag(directiveEnum))
                            fedDirectives |= directiveEnum;
                    }
                }
            }

            return fedDirectives;
        }
    }
}
