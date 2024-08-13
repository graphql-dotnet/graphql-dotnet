using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities;

/// <summary>
/// Validates the schema as required by the official specification. Also looks for
/// default values within arguments and inputs fields which are stored in AST nodes
/// and coerces them to their internally represented values.
/// </summary>
public sealed class SchemaValidationVisitor : BaseSchemaNodeVisitor
{
    private readonly List<Exception> _exceptions = new();

    private SchemaValidationVisitor()
    {
    }

    /// <inheritdoc cref="SchemaValidationVisitor"/>
    public static void Run(ISchema schema)
    {
        var visitor = new SchemaValidationVisitor();
        visitor.Run(schema);
        if (visitor._exceptions.Count > 0)
            throw visitor._exceptions.Count == 1
                ? visitor._exceptions[0]
                : new AggregateException(
                    "The schema is invalid. See inner exceptions for details.",
                    visitor._exceptions);
    }

    #region Object

    // See 'Type Validation' section in https://spec.graphql.org/October2021/#sec-Objects
    // Object types have the potential to be invalid if incorrectly defined.
    // This set of rules must be adhered to by every Object type in a GraphQL schema.
    /// <inheritdoc/>
    public override void VisitObject(IObjectGraphType type, ISchema schema)
    {
        // 1
        if (!type.IsPrivate && type.Fields.Count == 0)
            ReportError(new InvalidOperationException($"An Object type '{type.Name}' must define one or more fields."));

        // 2.1
        foreach (var item in type.Fields.List.ToLookup(f => f.Name))
        {
            if (item.Count() > 1)
                ReportError(new InvalidOperationException($"The field '{item.Key}' must have a unique name within Object type '{type.Name}'; no two fields may share the same name."));
        }

        // 3
        // TODO: ? An object type may declare that it implements one or more unique interfaces.

        // Implemented interfaces must be valid for the implementing type.
        foreach (var iface in type.ResolvedInterfaces.List)
        {
            try
            {
                iface.IsValidInterfaceFor(type, true);
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }

        // Transitively implemented interfaces (interfaces implemented by the interface that is being implemented) must also be defined on an implementing type or interface.
        CheckTransitiveInterfaces(type);
    }

    /// <inheritdoc/>
    public override void VisitObjectFieldDefinition(FieldType field, IObjectGraphType type, ISchema schema)
    {
        // 2.2
        if (field.Name.StartsWith("__"))
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' must not have a name which begins with the __ (two underscores)."));

        if (!HasFullSpecifiedResolvedType(field))
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain."));

        if (field.ResolvedType is GraphQLTypeReference)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference."));

        // 2.3
        if (!field.ResolvedType!.IsOutputType())
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' must be an output type."));

        ValidateFieldArgumentsUniqueness(field, type);

        if (field.StreamResolver != null && type != schema.Subscription)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' must not have StreamResolver set. You should set StreamResolver only for the root fields of subscriptions."));

        if (field.Parser != null)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' must not have Parser set. You should set Parser only for fields of input object types."));

        if (field.Validator != null)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Object type '{type.Name}' must not have Validator set. You should set Validator only for fields of input object types."));

        if (field.ResolvedType is IAbstractGraphType interfaceType && interfaceType.ResolveType == null)
        {
            foreach (var possibleType in interfaceType.PossibleTypes.List)
            {
                if (possibleType.IsTypeOf == null)
                {
                    ReportError(new InvalidOperationException(
                        $"Interface type '{interfaceType.Name}' does not provide a 'resolveType' function " +
                        $"and possible Type '{possibleType.Name}' does not provide a 'isTypeOf' function.  " +
                        "There is no way to resolve this possible type during execution."));
                }
            }
            interfaceType.ResolveType = (value) =>
            {
                foreach (var possible in interfaceType.PossibleTypes.List)
                {
                    if (possible.IsTypeOf != null && possible.IsTypeOf(value))
                        return possible;
                }

                return null;
            };
        }
    }

    /// <inheritdoc/>
    public override void VisitObjectFieldArgumentDefinition(QueryArgument argument, FieldType field, IObjectGraphType type, ISchema schema)
    {
        // 2.4.1
        if (argument.Name.StartsWith("__"))
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must not have a name which begins with the __ (two underscores)."));

        if (!HasFullSpecifiedResolvedType(argument))
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain."));

        if (argument.ResolvedType is GraphQLTypeReference)
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference."));

        // 2.4.2
        if (!argument.ResolvedType!.IsInputType())
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must be an input type."));

        // validate default value
        ValidateQueryArgumentDefaultValue(argument, field, type);

        // 2.4.3
        if (argument.ResolvedType is NonNullGraphType && argument.DefaultValue is null && argument.DeprecationReason is not null)
            ReportError(new InvalidOperationException($"The required argument '{argument.Name}' of field '{type.Name}.{field.Name}' has no default value so `@deprecated` directive must not be applied to this argument. To deprecate a required argument, it must first be made optional by either changing the type to nullable or adding a default value."));
    }

    #endregion

    #region Interface

    // See 'Type Validation' section in https://spec.graphql.org/October2021/#sec-Interfaces
    // Interface types have the potential to be invalid if incorrectly defined.
    /// <inheritdoc/>
    public override void VisitInterface(IInterfaceGraphType type, ISchema schema)
    {
        // 1
        if (!type.IsPrivate && type.Fields.Count == 0)
            ReportError(new InvalidOperationException($"An Interface type '{type.Name}' must define one or more fields."));

        // 2.1
        foreach (var item in type.Fields.List.ToLookup(f => f.Name))
        {
            if (item.Count() > 1)
                ReportError(new InvalidOperationException($"The field '{item.Key}' must have a unique name within Interface type '{type.Name}'; no two fields may share the same name."));
        }

        // Implemented interfaces must be valid for the implementing type.
        foreach (var iface in type.ResolvedInterfaces.List)
        {
            try
            {
                iface.IsValidInterfaceFor(type, true);
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }

        // Interface definitions must not contain cyclic references nor implement themselves.
        if (type.Interfaces.Count > 0)
        {
            var types = new HashSet<IInterfaceGraphType>() { type };
            CheckCyclicReferences(type);

            void CheckCyclicReferences(IInterfaceGraphType iface)
            {
                foreach (var i in iface.ResolvedInterfaces.List)
                {
                    if (types.Add(i))
                        CheckCyclicReferences(i);

                    if (i == type)
                    {
                        ReportError(new InvalidOperationException($"The interface type '{type.Name}' must not contain cyclic references."));
                        return;
                    }
                }
            }
        }

        // Transitively implemented interfaces (interfaces implemented by the interface that is being implemented) must also be defined on an implementing type or interface.
        CheckTransitiveInterfaces(type);
    }

    /// <summary>
    /// Ensures that all transitively implemented interfaces are defined on the implementing type.
    /// </summary>
    private void CheckTransitiveInterfaces(IImplementInterfaces type)
    {
        if (type.ResolvedInterfaces.Count == 0)
            return;

        var checkedInterfaces = new HashSet<IInterfaceGraphType>();
        CheckChildren(type);

        void CheckChildren(IImplementInterfaces iface)
        {
            foreach (var i in iface.ResolvedInterfaces.List)
            {
                if (checkedInterfaces.Add(i))
                {
                    if (!type.ResolvedInterfaces.Contains(i))
                        ReportError(new InvalidOperationException($"The interface type '{type.Name}' must also define all interfaces implemented by its transitive interfaces. The interface '{i.Name}' is implemented by the interface '{iface.Name}' but is not implemented by '{type.Name}'."));

                    CheckChildren(i);
                }
            }
        }
    }

    /// <inheritdoc/>
    public override void VisitInterfaceFieldDefinition(FieldType field, IInterfaceGraphType type, ISchema schema)
    {
        // 2.2
        if (field.Name.StartsWith("__"))
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' must not have a name which begins with the __ (two underscores)."));

        if (!HasFullSpecifiedResolvedType(field))
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain."));

        if (field.ResolvedType is GraphQLTypeReference)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference."));

        // 2.3
        if (!field.ResolvedType!.IsOutputType())
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' must be an output type."));

        ValidateFieldArgumentsUniqueness(field, type);

        if (field.StreamResolver != null && type != schema.Subscription)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' must not have StreamResolver set. You should set StreamResolver only for the root fields of subscriptions."));

        if (field.Resolver != null)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' must not have Resolver set. Each interface is translated to a concrete type during request execution. You should set Resolver only for fields of object output types."));

        if (field.Parser != null)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' must not have Parser set. Each interface is translated to a concrete type during request execution. You should set Parser only for fields of input object types."));

        if (field.Validator != null)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Interface type '{type.Name}' must not have Validator set. Each interface is translated to a concrete type during request execution. You should set Validator only for fields of input object types."));
    }

    /// <inheritdoc/>
    public override void VisitInterfaceFieldArgumentDefinition(QueryArgument argument, FieldType field, IInterfaceGraphType type, ISchema schema)
    {
        // 2.4.1
        if (argument.Name.StartsWith("__"))
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must not have a name which begins with the __ (two underscores)."));

        if (!HasFullSpecifiedResolvedType(argument))
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain."));

        if (argument.ResolvedType is GraphQLTypeReference)
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference."));

        // 2.4.2
        if (!argument.ResolvedType!.IsInputType())
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of field '{type.Name}.{field.Name}' must be an input type."));

        // validate default value
        ValidateQueryArgumentDefaultValue(argument, field, type);

        // 2.4.3
        if (argument.ResolvedType is NonNullGraphType && argument.DefaultValue is null && argument.DeprecationReason is not null)
            ReportError(new InvalidOperationException($"The required argument '{argument.Name}' of field '{type.Name}.{field.Name}' has no default value so `@deprecated` directive must not be applied to this argument. To deprecate a required argument, it must first be made optional by either changing the type to nullable or adding a default value."));
    }

    #endregion

    #region Input Object

    // See 'Type Validation' section in https://spec.graphql.org/October2021/#sec-Input-Objects
    // Input Object types have the potential to be invalid if incorrectly defined.
    /// <inheritdoc/>
    public override void VisitInputObject(IInputObjectGraphType type, ISchema schema)
    {
        // 1
        if (!type.IsPrivate && type.Fields.Count == 0)
            ReportError(new InvalidOperationException($"An Input Object type '{type.Name}' must define one or more input fields."));

        // 2.1
        foreach (var item in type.Fields.List.ToLookup(f => f.Name))
        {
            if (item.Count() > 1)
                ReportError(new InvalidOperationException($"The input field '{item.Key}' must have a unique name within Input Object type '{type.Name}'; no two fields may share the same name."));
        }
    }

    /// <inheritdoc/>
    public override void VisitInputObjectFieldDefinition(FieldType field, IInputObjectGraphType type, ISchema schema)
    {
        // 2.2
        if (field.Name.StartsWith("__"))
            ReportError(new InvalidOperationException($"The input field '{field.Name}' of an Input Object '{type.Name}' must not have a name which begins with the __ (two underscores)."));

        if (!HasFullSpecifiedResolvedType(field))
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Input Object type '{type.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain."));

        if (field.ResolvedType is GraphQLTypeReference)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Input Object type '{type.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference."));

        // 2.3
        if (!field.ResolvedType!.IsInputType())
            ReportError(new InvalidOperationException($"The input field '{field.Name}' of an Input Object '{type.Name}' must be an input type."));

        // validate default value
        if (field.DefaultValue is GraphQLValue value)
        {
            field.DefaultValue = Execution.ExecutionHelper.CoerceValue(field.ResolvedType!, value).Value;
        }
        else if (field.DefaultValue != null && !field.ResolvedType!.IsValidDefault(field.DefaultValue))
        {
            ReportError(new InvalidOperationException($"The default value of Input Object type field '{type.Name}.{field.Name}' is invalid."));
        }

        if (field.Arguments?.Count > 0)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Input Object type '{type.Name}' must not have any arguments specified."));

        // 2.4
        if (field.ResolvedType is NonNullGraphType && field.DefaultValue is null && field.DeprecationReason is not null)
            ReportError(new InvalidOperationException($"The required input field '{field.Name}' of an Input Object '{type.Name}' has no default value so `@deprecated` directive must not be applied to this input field. To deprecate an input field, it must first be made optional by either changing the type to nullable or adding a default value."));

        if (field.StreamResolver != null)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Input Object type '{type.Name}' must not have StreamResolver set. You should set StreamResolver only for the root fields of subscriptions."));

        if (field.Resolver != null)
            ReportError(new InvalidOperationException($"The field '{field.Name}' of an Input Object type '{type.Name}' must not have Resolver set. You should set Resolver only for fields of object output types."));

        //OneOf Input Objects
        // RULE: If the original Input Object is a OneOf Input Object then:
        // - All fields of the Input Object type extension must be nullable.
        // - All fields of the Input Object type extension must not have default values.
        if (type.IsOneOf)
        {
            if (field.ResolvedType is NonNullGraphType)
                ReportError(new InvalidOperationException($"The field '{field.Name}' of a OneOf Input Object type '{type.Name}' must be a nullable type."));
            if (field.DefaultValue != null)
                ReportError(new InvalidOperationException($"The field '{field.Name}' of a OneOf Input Object type '{type.Name}' must not have a default value."));
        }
    }

    #endregion

    // See https://spec.graphql.org/October2021/#sec-Root-Operation-Types
    /// <inheritdoc/>
    public override void VisitSchema(ISchema schema)
    {
        var n1 = schema.Query?.Name;
        var n2 = schema.Mutation?.Name;
        var n3 = schema.Subscription?.Name;
        if (n1 == n2 && n1 != null || n1 == n3 && n1 != null || n2 == n3 && n2 != null)
            ReportError(new InvalidOperationException("The query, mutation, and subscription root types must all be different types if provided."));
        if (schema.Subscription != null)
        {
            foreach (var field in schema.Subscription.Fields.List)
            {
                if (field.StreamResolver == null)
                    ReportError(new InvalidOperationException($"The field '{field.Name}' of the subscription root type '{schema.Subscription.Name}' must have StreamResolver set."));
            }
        }
    }

    // See 'Type Validation' section in https://spec.graphql.org/October2021/#sec-Unions
    // Union types have the potential to be invalid if incorrectly defined.
    /// <inheritdoc/>
    public override void VisitUnion(UnionGraphType type, ISchema schema)
    {
        // 1
        if (!type.IsPrivate && type.PossibleTypes.Count == 0)
            ReportError(new InvalidOperationException($"A Union type '{type.Name}' must include one or more unique member types."));

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
        if (!type.IsPrivate && type.Values.Count == 0)
            ReportError(new InvalidOperationException($"An Enum type '{type.Name}' must define one or more unique enum values."));
    }

    // See 'Type Validation' section in https://spec.graphql.org/October2021/#sec-Type-System.Directives
    // Directive types have the potential to be invalid if incorrectly defined.
    /// <inheritdoc/>
    public override void VisitDirective(Directive directive, ISchema schema)
    {
        if (directive.Locations.Count == 0)
            ReportError(new InvalidOperationException($"Directive '{directive.Name}' must have locations"));

        // 1. A directive definition must not contain the use of a directive which references itself directly.
        // TODO:

        // 2. A directive definition must not contain the use of a directive which references itself indirectly
        // by referencing a Type or Directive which transitively includes a reference to this directive.
        // TODO:

        // 3
        if (directive.Name.StartsWith("__"))
            ReportError(new InvalidOperationException($"The directive '{directive.Name}' must not have a name which begins with the __ (two underscores)."));

        ValidateDirectiveArgumentsUniqueness(directive);
    }

    /// <inheritdoc/>
    public override void VisitDirectiveArgumentDefinition(QueryArgument argument, Directive directive, ISchema schema)
    {
        // 4.1
        if (argument.Name.StartsWith("__"))
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of directive '{directive.Name}' must not have a name which begins with the __ (two underscores)."));

        if (!HasFullSpecifiedResolvedType(argument))
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of directive '{directive.Name}' must have non-null '{nameof(IFieldType.ResolvedType)}' property for all types in the chain."));

        if (argument.ResolvedType is GraphQLTypeReference)
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of directive '{directive.Name}' has '{nameof(GraphQLTypeReference)}' type. This type must be replaced with a reference to the actual GraphQL type before using the reference."));

        // 4.2
        if (!argument.ResolvedType!.IsInputType())
            ReportError(new InvalidOperationException($"The argument '{argument.Name}' of directive '{directive.Name}' must be an input type."));

        // validate default
        if (argument.DefaultValue is GraphQLValue value)
        {
            argument.DefaultValue = Execution.ExecutionHelper.CoerceValue(argument.ResolvedType!, value).Value;
        }
        else if (argument.DefaultValue != null && !argument.ResolvedType!.IsValidDefault(argument.DefaultValue))
        {
            ReportError(new InvalidOperationException($"The default value of argument '{argument.Name}' of directive '{directive.Name}' is invalid."));
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
            ReportError(new InvalidOperationException($"The default value of argument '{argument.Name}' of field '{type.Name}.{field.Name}' is invalid."));
        }
    }

    private void ValidateFieldArgumentsUniqueness(FieldType field, INamedType type)
    {
        if (field.Arguments?.Count > 0)
        {
            foreach (var item in field.Arguments.List!.ToLookup(f => f.Name))
            {
                if (item.Count() > 1)
                    ReportError(new InvalidOperationException($"The argument '{item.Key}' must have a unique name within field '{type.Name}.{field.Name}'; no two field arguments may share the same name."));
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
                    ReportError(new InvalidOperationException($"The argument '{item.Key}' must have a unique name within directive '{directive.Name}'; no two directive arguments may share the same name."));
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

    private void ReportError(Exception ex)
    {
        _exceptions.Add(ex);
    }
}
