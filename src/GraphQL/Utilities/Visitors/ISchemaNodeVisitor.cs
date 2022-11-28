using GraphQL.Types;

namespace GraphQL.Utilities
{
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
        void VisitSchema(ISchema schema);

        /// <summary>
        /// Visits registered within the schema <see cref="Directive"/>.
        /// </summary>
        void VisitDirective(Directive directive, ISchema schema);

        /// <summary>
        /// Visits registered within the schema <see cref="ScalarGraphType"/>.
        /// </summary>
        void VisitScalar(ScalarGraphType type, ISchema schema);

        /// <summary>
        /// Visits registered within the schema object graph type.
        /// </summary>
        void VisitObject(IObjectGraphType type, ISchema schema);

        /// <summary>
        /// Visits registered within the schema input object graph type.
        /// </summary>
        void VisitInputObject(IInputObjectGraphType type, ISchema schema);

        /// <summary>
        /// Visits field of registered within the schema object graph type.
        /// </summary>
        void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema);

        /// <summary>
        /// Visits field of registered within the schema interface graph type.
        /// </summary>
        void VisitInterfaceFieldDefinition(FieldType field, IInterfaceGraphType type, ISchema schema);

        /// <summary>
        /// Visits field of registered within the schema input object graph type.
        /// </summary>
        void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema);

        /// <summary>
        /// Visits field argument of registered within the schema object graph type.
        /// </summary>
        void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema);

        /// <summary>
        /// Visits field argument of registered within the schema interface graph type.
        /// </summary>
        void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema);

        /// <summary>
        /// Visits directive argument.
        /// </summary>
        void VisitDirectiveArgumentDefinition(QueryArgument argument, Directive directive, ISchema schema);

        /// <summary>
        /// Visits registered within the schema <see cref="IInterfaceGraphType"/>.
        /// </summary>
        void VisitInterface(IInterfaceGraphType type, ISchema schema);

        /// <summary>
        /// Visits registered within the schema <see cref="UnionGraphType"/>.
        /// </summary>
        void VisitUnion(UnionGraphType type, ISchema schema);

        /// <summary>
        /// Visits registered within the schema <see cref="EnumerationGraphType"/>.
        /// </summary>
        void VisitEnum(EnumerationGraphType type, ISchema schema);

        /// <summary>
        /// Visits value of registered within the schema <see cref="EnumerationGraphType"/>.
        /// </summary>
        void VisitEnumValue(EnumValueDefinition value, EnumerationGraphType type, ISchema schema);
    }
}
