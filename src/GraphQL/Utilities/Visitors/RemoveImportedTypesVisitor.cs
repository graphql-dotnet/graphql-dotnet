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

    /// <summary>
    /// Remove all type and directive definitions that are imported from another schema via the @link directive.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    /// <param name="schema">The schema containing the link directives.</param>
    public static void Visit(ASTNode node, ISchema schema)
        => Visit(node, schema, (string[])null!);

    /// <summary>
    /// Remove all type and directive definitions that are imported from another schema via the @link directive,
    /// filtering by URL prefixes. Only imports from URLs starting with the specified prefixes will be removed.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    /// <param name="schema">The schema containing the link directives.</param>
    /// <param name="urlPrefixes">Array of URL prefixes to filter by. Only links with URLs starting with these prefixes will have their imports removed.</param>
    public static void Visit(ASTNode node, ISchema schema, params string[] urlPrefixes)
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

            // Filter by URL prefixes if provided
            if (urlPrefixes != null)
            {
                bool matchesPrefix = false;
                foreach (var prefix in urlPrefixes)
                {
                    if (link.Url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        matchesPrefix = true;
                        break;
                    }
                }
                if (!matchesPrefix)
                    continue;
            }

            if (link.Namespace != null)
            {
                importedNamespaces ??= [];
                importedNamespaces.Add(link.Namespace + "__");
            }
            if (link.Imports?.Count > 0)
            {
                importedTypes ??= [];
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
