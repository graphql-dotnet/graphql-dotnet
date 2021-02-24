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
        public static readonly AppliedDirectivesValidationVisitor Instance = new AppliedDirectivesValidationVisitor();

        private AppliedDirectivesValidationVisitor()
        {
        }

        public void VisitFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(argument, schema, DirectiveLocation.ArgumentDefinition);

        public void VisitDirectiveArgumentDefinition(QueryArgument argument, DirectiveGraphType type, ISchema schema) => ValidateAppliedDirectives(argument, schema, DirectiveLocation.ArgumentDefinition);

        public void VisitEnum(EnumerationGraphType type, ISchema schema) => ValidateAppliedDirectives(type, schema, DirectiveLocation.Enum);

        public void VisitDirective(DirectiveGraphType directive, ISchema schema) => ValidateAppliedDirectives(directive, schema, null); // no location for directives (yet), see https://github.com/graphql/graphql-spec/issues/818

        public void VisitEnumValue(EnumValueDefinition value, EnumerationGraphType type, ISchema schema) => ValidateAppliedDirectives(value, schema, DirectiveLocation.EnumValue);

        public void VisitFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(field, schema, DirectiveLocation.FieldDefinition);

        public void VisitInputFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(field, schema, DirectiveLocation.InputFieldDefinition);

        public void VisitInputObject(IInputObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(type, schema, DirectiveLocation.InputObject);

        public void VisitInterface(IInterfaceGraphType iface, ISchema schema) => ValidateAppliedDirectives(iface, schema, DirectiveLocation.Interface);

        public void VisitObject(IObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(type, schema, DirectiveLocation.Object);

        public void VisitScalar(ScalarGraphType scalar, ISchema schema) => ValidateAppliedDirectives(scalar, schema, DirectiveLocation.Scalar);

        public void VisitSchema(ISchema schema) => ValidateAppliedDirectives(schema, schema, DirectiveLocation.Schema);

        public void VisitUnion(UnionGraphType union, ISchema schema) => ValidateAppliedDirectives(union, schema, DirectiveLocation.Union);

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

                    if (appliedDirective.Arguments?.Count > 0)
                    {
                        foreach (var arg in appliedDirective.Arguments)
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
