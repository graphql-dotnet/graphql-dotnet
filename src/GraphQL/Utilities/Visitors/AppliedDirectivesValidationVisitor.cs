using System;
using System.Linq;
using GraphQL.Types;

namespace GraphQL.Utilities
{
    /// <summary>
    /// This visitor verifies the correct application of directives to the schema elements.
    /// </summary>
    public sealed class AppliedDirectivesValidationVisitor : ISchemaNodeVisitor
    {
        /// <summary>
        /// Returns a static instance of the <see cref="AppliedDirectivesValidationVisitor"/> class.
        /// </summary>
        public static readonly AppliedDirectivesValidationVisitor Instance = new AppliedDirectivesValidationVisitor();

        private AppliedDirectivesValidationVisitor()
        {
        }

        /// <inheritdoc/>
        public void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(argument, schema, DirectiveLocation.ArgumentDefinition);

        /// <inheritdoc/>
        public void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema) => ValidateAppliedDirectives(argument, schema, DirectiveLocation.ArgumentDefinition);

        /// <inheritdoc/>
        public void VisitDirectiveArgumentDefinition(QueryArgument argument, DirectiveGraphType type, ISchema schema) => ValidateAppliedDirectives(argument, schema, DirectiveLocation.ArgumentDefinition);

        /// <inheritdoc/>
        public void VisitEnum(EnumerationGraphType type, ISchema schema) => ValidateAppliedDirectives(type, schema, DirectiveLocation.Enum);

        /// <inheritdoc/>
        public void VisitDirective(DirectiveGraphType type, ISchema schema) => ValidateAppliedDirectives(type, schema, null); // no location for directives (yet), see https://github.com/graphql/graphql-spec/issues/818

        /// <inheritdoc/>
        public void VisitEnumValue(EnumValueDefinition value, EnumerationGraphType type, ISchema schema) => ValidateAppliedDirectives(value, schema, DirectiveLocation.EnumValue);

        /// <inheritdoc/>
        public void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(field, schema, DirectiveLocation.FieldDefinition);

        /// <inheritdoc/>
        public void VisitInterfaceFieldDefinition(FieldType field, IInterfaceGraphType type, ISchema schema) => ValidateAppliedDirectives(field, schema, DirectiveLocation.FieldDefinition);

        /// <inheritdoc/>
        public void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(field, schema, DirectiveLocation.InputFieldDefinition);

        /// <inheritdoc/>
        public void VisitInputObject(IInputObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(type, schema, DirectiveLocation.InputObject);

        /// <inheritdoc/>
        public void VisitInterface(IInterfaceGraphType type, ISchema schema) => ValidateAppliedDirectives(type, schema, DirectiveLocation.Interface);

        /// <inheritdoc/>
        public void VisitObject(IObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(type, schema, DirectiveLocation.Object);

        /// <inheritdoc/>
        public void VisitScalar(ScalarGraphType type, ISchema schema) => ValidateAppliedDirectives(type, schema, DirectiveLocation.Scalar);

        /// <inheritdoc/>
        public void VisitSchema(ISchema schema) => ValidateAppliedDirectives(schema, schema, DirectiveLocation.Schema);

        /// <inheritdoc/>
        public void VisitUnion(UnionGraphType type, ISchema schema) => ValidateAppliedDirectives(type, schema, DirectiveLocation.Union);

        /// <inheritdoc/>
        private void ValidateAppliedDirectives(IProvideMetadata provider, ISchema schema, DirectiveLocation? location)
        {
            if (provider.HasAppliedDirectives())
            {
                var appliedDirectives = provider.GetAppliedDirectives().List;

                foreach (var directive in schema.Directives.List)
                {
                    if (!directive.Repeatable && appliedDirectives.Count(applied => applied.Name == directive.Name) > 1)
                        throw new InvalidOperationException($"Non-repeatable directive '{directive.Name}' is applied to element '{provider.GetType().Name}' more than one time.");
                }

                foreach (var appliedDirective in appliedDirectives)
                {
                    var schemaDirective = schema.Directives.Find(appliedDirective.Name);
                    if (schemaDirective == null)
                        throw new InvalidOperationException($"Unknown directive '{appliedDirective.Name}'.");

                    if (location != null && !schemaDirective.Locations.Contains(location.Value))
                        throw new InvalidOperationException($"Directive '{schemaDirective.Name}' is applied in the wrong location '{location.Value}'. Allowed locations: {string.Join(", ", schemaDirective.Locations)}");

                    if (schemaDirective.Arguments?.Count > 0)
                    {
                        foreach (var arg in schemaDirective.Arguments.List)
                        {
                            var a = appliedDirective.FindArgument(arg.Name);
                            if (a == null && arg.ResolvedType is NonNullGraphType && arg.DefaultValue == null)
                                throw new InvalidOperationException($"Directive '{appliedDirective.Name}' must specify required argument '{arg.Name}'.");

                            if (a != null)
                            {
                                //TODO: add check for applied directive argument value type
                                //a.Value should be of arg.ResolvedType / arg.Type
                            }
                        }
                    }

                    if (appliedDirective.ArgumentsCount > 0)
                    {
                        foreach (var arg in appliedDirective.List)
                        {
                            if (schemaDirective.Arguments?.Find(arg.Name) == null)
                                throw new InvalidOperationException($"Unknown directive argument '{arg.Name}' for directive '{appliedDirective.Name}'.");
                        }
                    }

                    schemaDirective.Validate(appliedDirective);
                }
            }
        }
    }
}
