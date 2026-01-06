using GraphQL.Types;

namespace GraphQL.Utilities;

/// <summary>
/// Visitor which methods are called when traversing the schema. This happens either explicitly, i.e. when calling
/// <see cref="SchemaExtensions.Run(ISchemaNodeVisitor, ISchema)"/> method directly or during schema creation when
/// this method is executed on all schema visitors registered on the schema.
/// <br/>
/// Also see <see href="https://www.apollographql.com/docs/graphql-tools/schema-directives/#implementing-schema-directives"/>
/// </summary>
public interface ISchemaNodeVisitor
{
    /// <summary>
    /// Visits <see cref="Schema"/>.
    /// </summary>
    public void VisitSchema(ISchema schema);

    /// <summary>
    /// Visits <see cref="Schema"/> after all other definitions have been visited.
    /// </summary>
    public void PostVisitSchema(ISchema schema);

    /// <summary>
    /// Visits registered within the schema <see cref="Directive"/>.
    /// </summary>
    public void VisitDirective(Directive directive, ISchema schema);

    /// <summary>
    /// Visits registered within the schema <see cref="ScalarGraphType"/>.
    /// </summary>
    public void VisitScalar(ScalarGraphType type, ISchema schema);

    /// <summary>
    /// Visits registered within the schema object graph type.
    /// </summary>
    public void VisitObject(IObjectGraphType type, ISchema schema);

    /// <summary>
    /// Visits registered within the schema input object graph type.
    /// </summary>
    public void VisitInputObject(IInputObjectGraphType type, ISchema schema);

    /// <summary>
    /// Visits field of registered within the schema object graph type.
    /// </summary>
    public void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema);

    /// <summary>
    /// Visits field of registered within the schema interface graph type.
    /// </summary>
    public void VisitInterfaceFieldDefinition(FieldType field, IInterfaceGraphType type, ISchema schema);

    /// <summary>
    /// Visits field of registered within the schema input object graph type.
    /// </summary>
    public void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema);

    /// <summary>
    /// Visits field argument of registered within the schema object graph type.
    /// </summary>
    public void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema);

    /// <summary>
    /// Visits field argument of registered within the schema interface graph type.
    /// </summary>
    public void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema);

    /// <summary>
    /// Visits directive argument.
    /// </summary>
    public void VisitDirectiveArgumentDefinition(QueryArgument argument, Directive directive, ISchema schema);

    /// <summary>
    /// Visits registered within the schema <see cref="IInterfaceGraphType"/>.
    /// </summary>
    public void VisitInterface(IInterfaceGraphType type, ISchema schema);

    /// <summary>
    /// Visits registered within the schema <see cref="UnionGraphType"/>.
    /// </summary>
    public void VisitUnion(UnionGraphType type, ISchema schema);

    /// <summary>
    /// Visits registered within the schema <see cref="EnumerationGraphType"/>.
    /// </summary>
    public void VisitEnum(EnumerationGraphType type, ISchema schema);

    /// <summary>
    /// Visits value of registered within the schema <see cref="EnumerationGraphType"/>.
    /// </summary>
    public void VisitEnumValue(EnumValueDefinition value, EnumerationGraphType type, ISchema schema);
}
