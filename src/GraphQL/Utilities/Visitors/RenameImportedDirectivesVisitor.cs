using GraphQL.Types;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// Visitor that renames imported directives based on the @link directive.
/// </summary>
public sealed class RenameImportedDirectivesVisitor : BaseSchemaNodeVisitor
{
    private readonly List<LinkConfiguration> _linkConfigurations;

    /// <inheritdoc cref="RenameImportedDirectivesVisitor"/>
    public RenameImportedDirectivesVisitor(List<LinkConfiguration> linkConfigurations)
    {
        _linkConfigurations = linkConfigurations;
    }

    /// <inheritdoc cref="SchemaExtensions.Run(ISchemaNodeVisitor, ISchema)"/>
    public static void Run(ISchema schema)
    {
        var appliedDirectives = schema.GetAppliedDirectives();
        if (appliedDirectives == null)
            return;

        List<LinkConfiguration>? links = null;
        foreach (var appliedDirective in appliedDirectives)
        {
            var link = LinkConfiguration.GetConfiguration(appliedDirective);
            if (link == null)
                continue;
            links ??= new();
            links.Add(link);
        }

        if (links == null)
            return;
        var visitor = new RenameImportedDirectivesVisitor(links);
        visitor.Run(schema);
    }

    /// <inheritdoc/>
    public override void VisitEnum(EnumerationGraphType type, ISchema schema) => VisitNode(type);
    /// <inheritdoc/>
    public override void VisitEnumValue(EnumValueDefinition value, EnumerationGraphType type, ISchema schema) => VisitNode(value);
    /// <inheritdoc/>
    public override void VisitInputObject(IInputObjectGraphType type, ISchema schema) => VisitNode(type);
    /// <inheritdoc/>
    public override void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema) => VisitNode(field);
    /// <inheritdoc/>
    public override void VisitInterface(IInterfaceGraphType type, ISchema schema) => VisitNode(type);
    /// <inheritdoc/>
    public override void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema) => VisitNode(argument);
    /// <inheritdoc/>
    public override void VisitInterfaceFieldDefinition(FieldType field, IInterfaceGraphType type, ISchema schema) => VisitNode(field);
    /// <inheritdoc/>
    public override void VisitObject(IObjectGraphType type, ISchema schema) => VisitNode(type);
    /// <inheritdoc/>
    public override void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema) => VisitNode(argument);
    /// <inheritdoc/>
    public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema) => VisitNode(field);
    /// <inheritdoc/>
    public override void VisitScalar(ScalarGraphType type, ISchema schema) => VisitNode(type);
    /// <inheritdoc/>
    public override void VisitSchema(ISchema schema) => VisitNode(schema);
    /// <inheritdoc/>
    public override void VisitUnion(UnionGraphType type, ISchema schema) => VisitNode(type);

    /// <summary>
    /// Scan all applied directives to see if any were imported from another schema, and if so,
    /// rename them with the appropriate alias as defined by the @link directive.
    /// </summary>
    private void VisitNode(IMetadataReader metadataReader)
    {
        var appliedDirectives = metadataReader.GetAppliedDirectives();
        if (appliedDirectives?.List == null)
            return;

        foreach (var appliedDirective in appliedDirectives.List)
        {
            if (appliedDirective.FromSchemaUrl == null)
                continue;

            foreach (var link in _linkConfigurations)
            {
                if (appliedDirective.FromSchemaUrl.EndsWith("/") ? link.Url.StartsWith(appliedDirective.FromSchemaUrl) : link.Url == appliedDirective.FromSchemaUrl)
                {
                    appliedDirective.Name = link.NameForDirective(appliedDirective.Name);
                    break;
                }
            }
        }
    }
}
