using GraphQL.Types;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// Remove all type and directive definitions that are imported from another schema.
/// </summary>
public sealed class RemoveImportedTypesVisitor : ASTVisitor<RemoveImportedTypesVisitor.Context>
{
    private static readonly RemoveImportedTypesVisitor _instance = new();

    private RemoveImportedTypesVisitor()
    {
    }

    /// <inheritdoc cref="RemoveFederationTypesVisitor"/>
    public static void Visit(ASTNode node, ISchema schema)
    {
        var appliedDirectives = schema.GetAppliedDirectives();
        if (appliedDirectives == null)
            return;

        List<string>? importedNamespaces = null;
        HashSet<string>? importedTypes = null;
        foreach (var appliedDirective in appliedDirectives)
        {
            var link = LinkConfiguration.GetConfiguration(appliedDirective);
            if (link == null)
                continue;
            if (link.Namespace != null)
            {
                importedNamespaces ??= new();
                importedNamespaces.Add(link.Namespace + "__");
            }
            if (link.Imports?.Count > 0)
            {
                importedTypes ??= new();
                foreach (var import in link.Imports)
                {
                    importedTypes.Add(import.Value);
                }
            }
        }

        var context = new Context
        {
            ImportedNamespaces = importedNamespaces,
            ImportedTypes = importedTypes,
        };
        _instance.VisitAsync(node, context).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    protected override ValueTask VisitDocumentAsync(GraphQLDocument document, Context context)
    {
        // remove all federation directives and federation types
        document.Definitions.RemoveAll(node => node switch
        {
            GraphQLDirectiveDefinition directive => MatchDirectiveName(directive.Name.StringValue),
            GraphQLTypeDefinition type => MatchTypeName(type.Name.StringValue),
            _ => false,
        });

        return default;

        bool MatchDirectiveName(string directiveName)
        {
            if (context.ImportedNamespaces != null)
            {
                foreach (var importedNamespace in context.ImportedNamespaces)
                {
                    if (directiveName.StartsWith(importedNamespace))
                        return true;
                }
            }
            return context.ImportedTypes?.Contains("@" + directiveName) ?? false;
        }

        bool MatchTypeName(string typeName)
        {
            if (context.ImportedNamespaces != null)
            {
                foreach (var importedNamespace in context.ImportedNamespaces)
                {
                    if (typeName.StartsWith(importedNamespace))
                        return true;
                }
            }
            return context.ImportedTypes?.Contains(typeName) ?? false;
        }
    }

    /// <summary>
    /// Context for <see cref="RemoveImportedTypesVisitor"/>.
    /// </summary>
    public struct Context : IASTVisitorContext
    {
        /// <summary>
        /// Contains a list of imported namespaces.
        /// </summary>
        public List<string>? ImportedNamespaces { get; set; }

        /// <summary>
        ///  Contains a list of imported types and directives.
        /// </summary>
        public HashSet<string>? ImportedTypes { get; set; }

        /// <inheritdoc/>
        public CancellationToken CancellationToken => default;
    }
}
