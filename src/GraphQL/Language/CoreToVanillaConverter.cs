using System.Threading;
using System.Threading.Tasks;
using GraphQL.Utilities;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Language
{
    /// <summary>
    /// Converts an GraphQLParser AST representation of a document into a GraphQL.NET AST
    /// representation of a document. Works only with executable definitions - operations
    /// and fragments, all other definitions are ignored.
    /// <br/>
    /// For more information see https://spec.graphql.org/June2018/#sec-Language.Document.
    /// </summary>
    public class CoreToVanillaConverter : DefaultNodeVisitor<CoreToVanillaConverterContext>
    {
        //TODO: throw on schema definitions
        public override async ValueTask Visit(ASTNode? node, CoreToVanillaConverterContext context)
        {
            if (node is GraphQLEnumValue enumValue)
                NameValidator.ValidateDefault(enumValue.Name, NamedElement.EnumValue);

            await base.Visit(node, context).ConfigureAwait(false);
        }
    }

    public class CoreToVanillaConverterContext : INodeVisitorContext
    {
        public CancellationToken CancellationToken => default;
    }
}
