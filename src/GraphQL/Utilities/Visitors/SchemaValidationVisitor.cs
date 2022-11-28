using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Validates the schema as required by the official specification. Also looks for
    /// default values within arguments and inputs fields which are stored in AST nodes
    /// and coerces them to their internally represented values.
    /// </summary>
    public sealed class SchemaValidationVisitor : BaseSchemaNodeVisitor
    {
        /// <summary>
        /// Returns a static instance of the <see cref="SchemaValidationVisitor"/> class.
        /// </summary>
        public static readonly SchemaValidationVisitor Instance = new();

        private SchemaValidationVisitor()
        {
        }

        #region Object

        // See 'Type Validation' section in https://spec.graphql.org/October2021/#sec-Objects
        // Object types have the potential to be invalid if incorrectly defined.
        // This set of rules must be adhered to by every Object type in a GraphQL schema.
        /// <inheritdoc/>
        public override void VisitObject(IObjectGraphType type, ISchema schema)
        {
            // 1
            if (type.Fields.Count == 0)
                throw new InvalidOperationException($"An Object type '{type.Name}' must define one or more fields.");

            // 2.1
            foreach (var item in type.Fields.List.ToLookup(f => f.Name))
            {
                if (item.Count() > 1)
                    throw new InvalidOperationException($"The field '{item.Key}' must have a unique name within Object type '{type.Name}'; no two fields may share the same name.");
            }

            // 3
            // TODO: ? An object type may declare that it implements one or more unique interfaces.

            // 4
            // TODO: An object type must be a super‐set of all interfaces it implements
        }

        /// <inheritdoc/>
        public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
        {
            // 2.2
            if (field.Name.StartsWith("__"))
                throw new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' must not have a name which begins with the __ (two underscores).");

            if (!HasFullSpecifiedResolvedType(field))
                throw new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain.");

            if (field.ResolvedType is GraphQLTypeReference)
                throw new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference.");

            // 2.3
            if (!field.ResolvedType!.IsOutputType())
                throw new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' must be an output type.");

            ValidateFieldArgumentsUniqueness(field, type);
        }

        /// <inheritdoc/>
        public override void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema)
        {
            // 2.4.1
            if (argument.Name.StartsWith("__"))
                throw new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must not have a name which begins with the __ (two underscores).");

            if (!HasFullSpecifiedResolvedType(argument))
                throw new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain.");

            if (argument.ResolvedType is GraphQLTypeReference)
                throw new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference.");

            // 2.4.2
            if (!argument.ResolvedType!.IsInputType())
                throw new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must be an input type.");

            // validate default value
            ValidateQueryArgumentDefaultValue(argument, field, type);
        }

        #endregion

        #region Interface

        // See 'Type Validation' section in https://spec.graphql.org/October2021/#sec-Interfaces
        // Interface types have the potential to be invalid if incorrectly defined.
        /// <inheritdoc/>
        public override void VisitInterface(IInterfaceGraphType type, ISchema schema)
        {
            // 1
            if (type.Fields.Count == 0)
                throw new InvalidOperationException($"An Interface type '{type.Name}' must define one or more fields.");

            // 2.1
            foreach (var item in type.Fields.List.ToLookup(f => f.Name))
            {
                if (item.Count() > 1)
                    throw new InvalidOperationException($"The field '{item.Key}' must have a unique name within Interface type '{type.Name}'; no two fields may share the same name.");
            }
        }

        /// <inheritdoc/>
        public override void VisitInterfaceFieldDefinition(FieldType field, IInterfaceGraphType type, ISchema schema)
        {
            // 2.2
            if (field.Name.StartsWith("__"))
                throw new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' must not have a name which begins with the __ (two underscores).");

            if (!HasFullSpecifiedResolvedType(field))
                throw new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain.");

            if (field.ResolvedType is GraphQLTypeReference)
                throw new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference.");

            // 2.3
            if (!field.ResolvedType!.IsOutputType())
                throw new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' must be an output type.");

            ValidateFieldArgumentsUniqueness(field, type);
        }

        /// <inheritdoc/>
        public override void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema)
        {
            // 2.4.1
            if (argument.Name.StartsWith("__"))
                throw new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must not have a name which begins with the __ (two underscores).");

            if (!HasFullSpecifiedResolvedType(argument))
                throw new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain.");

            if (argument.ResolvedType is GraphQLTypeReference)
                throw new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference.");

            // 2.4.2
            if (!argument.ResolvedType!.IsInputType())
                throw new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must be an input type.");

            // validate default value
            ValidateQueryArgumentDefaultValue(argument, field, type);
        }

        #endregion

        #region Input Object

        // See 'Type Validation' section in https://spec.graphql.org/October2021/#sec-Input-Objects
        // Input Object types have the potential to be invalid if incorrectly defined.
        /// <inheritdoc/>
        public override void VisitInputObject(IInputObjectGraphType type, ISchema schema)
        {
            // 1
            if (type.Fields.Count == 0)
                throw new InvalidOperationException($"An Input Object type '{type.Name}' must define one or more input fields.");

            // 2.1
            foreach (var item in type.Fields.List.ToLookup(f => f.Name))
            {
                if (item.Count() > 1)
                    throw new InvalidOperationException($"The input field '{item.Key}' must have a unique name within Input Object type '{type.Name}'; no two fields may share the same name.");
            }
        }

        /// <inheritdoc/>
        public override void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema)
        {
            // 2.2
            if (field.Name.StartsWith("__"))
                throw new InvalidOperationException($"The input field '{field.Name}' of an Input Object '{type.Name}' must not have a name which begins with the __ (two underscores).");

            if (!HasFullSpecifiedResolvedType(field))
                throw new InvalidOperationException($"The field '{field.Name}' of an Input Object type '{type.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain.");

            if (field.ResolvedType is GraphQLTypeReference)
                throw new InvalidOperationException($"The field '{field.Name}' of an Input Object type '{type.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference.");

            // 2.3
            if (!field.ResolvedType!.IsInputType())
                throw new InvalidOperationException($"The input field '{field.Name}' of an Input Object '{type.Name}' must be an input type.");

            // validate default value
            if (field.DefaultValue is GraphQLValue value)
            {
                field.DefaultValue = Execution.ExecutionHelper.CoerceValue(field.ResolvedType!, value).Value;
            }
            else if (field.DefaultValue != null && !field.ResolvedType!.IsValidDefault(field.DefaultValue))
            {
                throw new InvalidOperationException($"The default value of Input Object type field '{type.Name}.{field.Name}' is invalid.");
            }

            if (field.Arguments?.Count > 0)
                throw new InvalidOperationException($"The field '{field.Name}' of an Input Object type '{type.Name}' must not have any arguments specified.");
        }

        #endregion

        // See 'Type Validation' section in https://spec.graphql.org/October2021/#sec-Unions
        // Union types have the potential to be invalid if incorrectly defined.
        /// <inheritdoc/>
        public override void VisitUnion(UnionGraphType type, ISchema schema)
        {
            // 1
            if (type.PossibleTypes.Count == 0)
                throw new InvalidOperationException($"A Union type '{type.Name}' must include one or more unique member types.");

            // 2 [requirement met by design]
            // The member types of a Union type must all be Object base types;
            // Scalar, Interface and Union types must not be member types of a Union.
            // Similarly, wrapping types must not be member types of a Union.
        }

        // See 'Type Validation' section in https://spec.graphql.org/October2021/#sec-Enums
        // Enum types have the potential to be invalid if incorrectly defined.
        /// <inheritdoc/>
        public override void VisitEnum(EnumerationGraphType type, ISchema schema)
        {
            // 1
            if (type.Values.Count == 0)
                throw new InvalidOperationException($"An Enum type '{type.Name}' must define one or more unique enum values.");
        }

        // See 'Type Validation' section in https://spec.graphql.org/October2021/#sec-Type-System.Directives
        // Directive types have the potential to be invalid if incorrectly defined.
        /// <inheritdoc/>
        public override void VisitDirective(Directive directive, ISchema schema)
        {
            if (directive.Locations.Count == 0)
                throw new InvalidOperationException($"Directive '{directive.Name}' must have locations");

            // 1. A directive definition must not contain the use of a directive which references itself directly.
            // TODO:

            // 2. A directive definition must not contain the use of a directive which references itself indirectly
            // by referencing a Type or Directive which transitively includes a reference to this directive.
            // TODO:

            // 3
            if (directive.Name.StartsWith("__"))
                throw new InvalidOperationException($"The directive '{directive.Name}' must not have a name which begins with the __ (two underscores).");

            ValidateDirectiveArgumentsUniqueness(directive);
        }

        /// <inheritdoc/>
        public override void VisitDirectiveArgumentDefinition(QueryArgument argument, Directive directive, ISchema schema)
        {
            // 4.1
            if (argument.Name.StartsWith("__"))
                throw new InvalidOperationException($"The argument '{argument.Name}' of directive '{directive.Name}' must not have a name which begins with the __ (two underscores).");

            if (!HasFullSpecifiedResolvedType(argument))
                throw new InvalidOperationException($"The argument '{argument.Name}' of directive '{directive.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain.");

            if (argument.ResolvedType is GraphQLTypeReference)
                throw new InvalidOperationException($"The argument '{argument.Name}' of directive '{directive.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference.");

            // 4.2
            if (!argument.ResolvedType!.IsInputType())
                throw new InvalidOperationException($"The argument '{argument.Name}' of directive '{directive.Name}' must be an input type.");

            // validate default
            if (argument.DefaultValue is GraphQLValue value)
            {
                argument.DefaultValue = Execution.ExecutionHelper.CoerceValue(argument.ResolvedType!, value).Value;
            }
            else if (argument.DefaultValue != null && !argument.ResolvedType!.IsValidDefault(argument.DefaultValue))
            {
                throw new InvalidOperationException($"The default value of argument '{argument.Name}' of directive '{directive.Name}' is invalid.");
            }
        }

        private void ValidateQueryArgumentDefaultValue(QueryArgument argument, FieldType field, INamedType type)
        {
            if (argument.DefaultValue is GraphQLValue value)
            {
                argument.DefaultValue = Execution.ExecutionHelper.CoerceValue(argument.ResolvedType!, value).Value;
            }
            else if (argument.DefaultValue != null && !argument.ResolvedType!.IsValidDefault(argument.DefaultValue))
            {
                throw new InvalidOperationException($"The default value of argument '{argument.Name}' of field '{type.Name}.{field.Name}' is invalid.");
            }
        }

        private void ValidateFieldArgumentsUniqueness(FieldType field, INamedType type)
        {
            if (field.Arguments?.Count > 0)
            {
                foreach (var item in field.Arguments.List!.ToLookup(f => f.Name))
                {
                    if (item.Count() > 1)
                        throw new InvalidOperationException($"The argument '{item.Key}' must have a unique name within field '{type.Name}.{field.Name}'; no two field arguments may share the same name.");
                }
            }
        }

        private void ValidateDirectiveArgumentsUniqueness(Directive directive)
        {
            if (directive.Arguments?.Count > 0)
            {
                foreach (var item in directive.Arguments.List!.ToLookup(f => f.Name))
                {
                    if (item.Count() > 1)
                        throw new InvalidOperationException($"The argument '{item.Key}' must have a unique name within directive '{directive.Name}'; no two directive arguments may share the same name.");
                }
            }
        }

        private static bool HasFullSpecifiedResolvedType(IProvideResolvedType type)
        {
            return type.ResolvedType switch
            {
                null => false,
                ListGraphType list => HasFullSpecifiedResolvedType(list),
                NonNullGraphType nonNull => HasFullSpecifiedResolvedType(nonNull),
                _ => true, // not null
            };
        }
    }
}
