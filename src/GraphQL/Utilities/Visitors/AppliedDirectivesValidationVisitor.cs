using GraphQL.Types;
using GraphQLParser.AST;

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
        public static readonly AppliedDirectivesValidationVisitor Instance = new();

        private AppliedDirectivesValidationVisitor()
        {
        }

        /// <inheritdoc/>
        public void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(argument, field, type, schema, DirectiveLocation.ArgumentDefinition);

        /// <inheritdoc/>
        public void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema) => ValidateAppliedDirectives(argument, field, type, schema, DirectiveLocation.ArgumentDefinition);

        /// <inheritdoc/>
        public void VisitDirectiveArgumentDefinition(QueryArgument argument, Directive directive, ISchema schema) => ValidateAppliedDirectives(argument, directive, null, schema, DirectiveLocation.ArgumentDefinition);

        /// <inheritdoc/>
        public void VisitEnum(EnumerationGraphType type, ISchema schema) => ValidateAppliedDirectives(type, null, null, schema, DirectiveLocation.Enum);

        /// <inheritdoc/>
        public void VisitDirective(Directive directive, ISchema schema) => ValidateAppliedDirectives(directive, null, null, schema, null); // no location for directives (yet), see https://github.com/graphql/graphql-spec/issues/818

        /// <inheritdoc/>
        public void VisitEnumValue(EnumValueDefinition value, EnumerationGraphType type, ISchema schema) => ValidateAppliedDirectives(value, type, null, schema, DirectiveLocation.EnumValue);

        /// <inheritdoc/>
        public void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(field, type, null, schema, DirectiveLocation.FieldDefinition);

        /// <inheritdoc/>
        public void VisitInterfaceFieldDefinition(FieldType field, IInterfaceGraphType type, ISchema schema) => ValidateAppliedDirectives(field, type, null, schema, DirectiveLocation.FieldDefinition);

        /// <inheritdoc/>
        public void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(field, type, null, schema, DirectiveLocation.InputFieldDefinition);

        /// <inheritdoc/>
        public void VisitInputObject(IInputObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(type, null, null, schema, DirectiveLocation.InputObject);

        /// <inheritdoc/>
        public void VisitInterface(IInterfaceGraphType type, ISchema schema) => ValidateAppliedDirectives(type, null, null, schema, DirectiveLocation.Interface);

        /// <inheritdoc/>
        public void VisitObject(IObjectGraphType type, ISchema schema) => ValidateAppliedDirectives(type, null, null, schema, DirectiveLocation.Object);

        /// <inheritdoc/>
        public void VisitScalar(ScalarGraphType type, ISchema schema) => ValidateAppliedDirectives(type, null, null, schema, DirectiveLocation.Scalar);

        /// <inheritdoc/>
        public void VisitSchema(ISchema schema) => ValidateAppliedDirectives(schema, null, null, schema, DirectiveLocation.Schema);

        /// <inheritdoc/>
        public void VisitUnion(UnionGraphType type, ISchema schema) => ValidateAppliedDirectives(type, null, null, schema, DirectiveLocation.Union);

        /// <inheritdoc/>
        private void ValidateAppliedDirectives(IProvideMetadata provider, object? parent1, object? parent2, ISchema schema, DirectiveLocation? location)
        {
            //TODO: switch to the schema coordinates?
            string GetElementDescription() => (provider, parent1, parent2) switch
            {
                (QueryArgument arg, FieldType field, INamedType type) => $"field argument '{type.Name}.{field.Name}:{arg.Name}'",
                (QueryArgument arg, Directive dir, _) => $"directive argument '{dir.Name}:{arg.Name}'",
                (EnumerationGraphType en, _, _) => $"enumeration '{en.Name}'",
                (EnumValueDefinition enVal, EnumerationGraphType type, _) => $"enumeration value '{type.Name}.{enVal.Name}'",
                (ScalarGraphType scalar, _, _) => $"scalar '{scalar.Name}'",
                (Directive dir, _, _) => $"directive '{dir.Name}'",
                (FieldType field, INamedType type, _) => $"field '{type.Name}.{field.Name}'",
                (IInputObjectGraphType input, _, _) => $"input '{input.Name}'",
                (IInterfaceGraphType iface, _, _) => $"interface '{iface.Name}'",
                (IObjectGraphType obj, _, _) => $"object '{obj.Name}'",
                (ISchema _, _, _) => "schema",
                _ => throw new NotSupportedException(provider.GetType().Name),
            };

            if (provider.HasAppliedDirectives())
            {
                var appliedDirectives = provider.GetAppliedDirectives()!.List!;

                foreach (var directive in schema.Directives.List)
                {
                    if (!directive.Repeatable && appliedDirectives.Count(applied => applied.Name == directive.Name) > 1)
                        throw new InvalidOperationException($"Non-repeatable directive '{directive.Name}' is applied to {GetElementDescription()} more than one time.");
                }

                foreach (var appliedDirective in appliedDirectives)
                {
                    var schemaDirective = schema.Directives.Find(appliedDirective.Name);
                    if (schemaDirective == null)
                        throw new InvalidOperationException($"Unknown directive '{appliedDirective.Name}' applied to {GetElementDescription()}.");

                    if (location != null && !schemaDirective.Locations.Contains(location.Value))
                    {
                        // TODO: think about strict check; needs to rewrite some tests (5)
                        // This is a temporary solution for @deprecated directive that we actually allow to more schema elements.
                        if (schemaDirective.Name != "deprecated")
                            throw new InvalidOperationException($"Directive '{schemaDirective.Name}' is applied in the wrong location '{location.Value}' to {GetElementDescription()}. Allowed locations: {string.Join(", ", schemaDirective.Locations)}");
                    }

                    if (schemaDirective.Arguments?.Count > 0)
                    {
                        foreach (var definedArg in schemaDirective.Arguments!.List!)
                        {
                            var appliedArg = appliedDirective.FindArgument(definedArg.Name);
                            if (appliedArg == null)
                            {
                                // definedArg.DefaultValue has been already validated in SchemaValidationVisitor.VisitDirectiveArgumentDefinition
                                if (definedArg.ResolvedType is NonNullGraphType && definedArg.DefaultValue == null)
                                    throw new InvalidOperationException($"Directive '{appliedDirective.Name}' applied to {GetElementDescription()} does not specify required argument '{definedArg.Name}' that has no default value.");
                            }
                            else
                            {
                                if (definedArg.ResolvedType is NonNullGraphType && appliedArg.Value == null)
                                    throw new InvalidOperationException($"Directive '{appliedDirective.Name}' applied to {GetElementDescription()} explicitly specifies 'null' value for required argument '{definedArg.Name}'. The value must be non-null.");
                            }

                            //TODO: add check for applied directive argument value type
                            //appliedArg.Value should be of definedArg.ResolvedType / definedArg.Type
                        }
                    }

                    if (appliedDirective.ArgumentsCount > 0)
                    {
                        foreach (var arg in appliedDirective.List!)
                        {
                            if (schemaDirective.Arguments?.Find(arg.Name) == null)
                                throw new InvalidOperationException($"Unknown directive argument '{arg.Name}' for directive '{appliedDirective.Name}' applied to {GetElementDescription()}.");
                        }
                    }

                    schemaDirective.Validate(appliedDirective);
                }
            }
        }
    }
}
