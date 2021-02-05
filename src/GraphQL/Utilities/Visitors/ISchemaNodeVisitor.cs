using GraphQL.Types;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Visitor which methods are called when traversing the schema either calling <see cref="SchemaExtensions.Run(ISchema, ISchemaNodeVisitor)"/>
    /// directly or during building schema via <see cref="SchemaBuilder.Build(string)"/>.
    /// <br/>
    /// Also see <see href="https://www.apollographql.com/docs/graphql-tools/schema-directives/#implementing-schema-directives"/>
    /// </summary>
    public interface ISchemaNodeVisitor
    {
        /// <summary>
        /// Visits <see cref="Schema"/> object.
        /// </summary>
        void VisitSchema(ISchema schema);

        /// <summary>
        /// Visits registered within the schema <see cref="DirectiveGraphType"/>.
        /// </summary>
        void VisitDirective(DirectiveGraphType directive, ISchema schema);

        /// <summary>
        /// Visits registered within the schema <see cref="ScalarGraphType"/>.
        /// </summary>
        void VisitScalar(ScalarGraphType scalar, ISchema schema);

        /// <summary>
        /// Visits registered within the schema output graph type.
        /// </summary>
        void VisitObject(IObjectGraphType type, ISchema schema);

        /// <summary>
        /// Visits registered within the schema input graph type.
        /// </summary>
        void VisitInputObject(IInputObjectGraphType type, ISchema schema);

        /// <summary>
        /// Visits field of registered within the schema output graph type.
        /// </summary>
        void VisitFieldDefinition(FieldType field, ISchema schema);

        /// <summary>
        /// Visits field of registered within the schema input graph type.
        /// </summary>
        void VisitInputFieldDefinition(FieldType field, ISchema schema);

        /// <summary>
        /// Visits field argument of registered within the schema graph type.
        /// </summary>
        void VisitFieldArgumentDefinition(QueryArgument argument, ISchema schema);

        /// <summary>
        /// Visits directive argument.
        /// </summary>
        void VisitDirectiveArgumentDefinition(QueryArgument argument, ISchema schema);

        /// <summary>
        /// Visits registered within the schema <see cref="IInterfaceGraphType"/>.
        /// </summary>
        void VisitInterface(IInterfaceGraphType iface, ISchema schema);

        /// <summary>
        /// Visits registered within the schema <see cref="UnionGraphType"/>.
        /// </summary>
        void VisitUnion(UnionGraphType union, ISchema schema);

        /// <summary>
        /// Visits registered within the schema <see cref="EnumerationGraphType"/>.
        /// </summary>
        void VisitEnum(EnumerationGraphType type, ISchema schema);

        /// <summary>
        /// Visits value of registered within the schema <see cref="EnumerationGraphType"/>.
        /// </summary>
        void VisitEnumValue(EnumValueDefinition value, ISchema schema);
    }
}
