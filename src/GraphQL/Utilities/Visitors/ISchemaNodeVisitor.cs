using GraphQL.Types;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Visitor which methods are called when traversing the schema either calling <see cref="SchemaExtensions.Run(Schema, ISchemaNodeVisitor)"/>
    /// directly or during building schema via <see cref="SchemaBuilder.Build(string)"/>.
    /// <br/>
    /// Also see <see href="https://www.apollographql.com/docs/graphql-tools/schema-directives/#implementing-schema-directives"/>
    /// </summary>
    public interface ISchemaNodeVisitor
    {
        /// <summary>
        /// Visits <see cref="Schema"/> object.
        /// </summary>
        void VisitSchema(Schema schema);

        /// <summary>
        /// Visits registered within the schema <see cref="DirectiveGraphType"/>.
        /// </summary>
        void VisitDirective(DirectiveGraphType directive);

        /// <summary>
        /// Visits registered within the schema <see cref="ScalarGraphType"/>.
        /// </summary>
        void VisitScalar(ScalarGraphType scalar);

        /// <summary>
        /// Visits registered within the schema output graph type.
        /// </summary>
        void VisitObject(IObjectGraphType type);

        /// <summary>
        /// Visits registered within the schema input graph type.
        /// </summary>
        void VisitInputObject(IInputObjectGraphType type);

        /// <summary>
        /// Visits field of registered within the schema output graph type.
        /// </summary>
        void VisitFieldDefinition(FieldType field);

        /// <summary>
        /// Visits field of registered within the schema input graph type.
        /// </summary>
        void VisitInputFieldDefinition(FieldType field);

        /// <summary>
        /// Visits field argument of registered within the schema graph type.
        /// </summary>
        void VisitFieldArgumentDefinition(QueryArgument argument);

        /// <summary>
        /// Visits directive argument.
        /// </summary>
        void VisitDirectiveArgumentDefinition(QueryArgument argument);

        /// <summary>
        /// Visits registered within the schema <see cref="IInterfaceGraphType"/>.
        /// </summary>
        void VisitInterface(IInterfaceGraphType iface);

        /// <summary>
        /// Visits registered within the schema <see cref="UnionGraphType"/>.
        /// </summary>
        void VisitUnion(UnionGraphType union);

        /// <summary>
        /// Visits registered within the schema <see cref="EnumerationGraphType"/>.
        /// </summary>
        void VisitEnum(EnumerationGraphType type);

        /// <summary>
        /// Visits value of registered within the schema <see cref="EnumerationGraphType"/>.
        /// </summary>
        void VisitEnumValue(EnumValueDefinition value);
    }
}
