using GraphQL.Types;
using GraphQLParser;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// A visitor that replaces all <see cref="GraphQLTypeReference"/> instances
/// with actual types from the schema's type dictionary.
/// </summary>
internal struct TypeReferenceReplacementVisitor
{
    private readonly Dictionary<ROM, IGraphType> _typeDictionary;
    private readonly Dictionary<string, ScalarGraphType> _builtInTypes;
    private readonly ISchema _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeReferenceReplacementVisitor"/> struct.
    /// </summary>
    /// <param name="typeDictionary">The dictionary containing all registered types in the schema.</param>
    /// <param name="builtInTypes">The dictionary containing all built-in scalar types.</param>
    /// <param name="schema">The schema being processed.</param>
    public TypeReferenceReplacementVisitor(Dictionary<ROM, IGraphType> typeDictionary, Dictionary<string, ScalarGraphType> builtInTypes, ISchema schema)
    {
        _typeDictionary = typeDictionary ?? throw new ArgumentNullException(nameof(typeDictionary));
        _builtInTypes = builtInTypes ?? throw new ArgumentNullException(nameof(builtInTypes));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    /// <summary>
    /// Runs the visitor on all types in the dictionary and directives in the schema.
    /// </summary>
    public void Run()
    {
        // Process directives
        foreach (var directive in _schema.Directives.List)
        {
            if (directive.Arguments?.Count > 0)
            {
                foreach (var argument in directive.Arguments.List!)
                {
                    argument.ResolvedType = ConvertTypeReference(directive, argument.ResolvedType!);
                }
            }
        }

        // Process all types in the dictionary
        foreach (var type in _typeDictionary.Values.ToList()) // copy list in case resovling GraphQLTypeReference adds a built-in scalar
        {
            switch (type)
            {
                case IComplexGraphType complexType:
                    VisitComplexType(complexType);
                    break;

                case UnionGraphType union:
                    VisitUnion(union);
                    break;
            }
        }
    }

    /// <summary>
    /// Visits a complex type (object, interface, or input object) and replaces type references in its fields.
    /// </summary>
    private void VisitComplexType(IComplexGraphType type)
    {
        // Replace field type references
        foreach (var field in type.Fields.List)
        {
            field.ResolvedType = ConvertTypeReference(type, field.ResolvedType!);

            // Replace argument type references
            if (field.Arguments?.Count > 0)
            {
                foreach (var argument in field.Arguments.List!)
                {
                    argument.ResolvedType = ConvertTypeReference(type, argument.ResolvedType!);
                }
            }
        }

        // Replace interface references for object and interface types
        if (type is IImplementInterfaces implementer && implementer.ResolvedInterfaces != null)
        {
            var list = implementer.ResolvedInterfaces.List;
            for (int i = 0; i < list.Count; ++i)
            {
                var interfaceType = (IInterfaceGraphType)ConvertTypeReference(type, list[i]);

                // Add possible type relationship for object types
                if (type is IObjectGraphType objectType)
                {
                    interfaceType.AddPossibleType(objectType);
                }

                list[i] = interfaceType;
            }
        }
    }

    /// <summary>
    /// Visits a union type and replaces type references in its possible types.
    /// </summary>
    private void VisitUnion(UnionGraphType type)
    {
        if (type.PossibleTypes != null)
        {
            var list = type.PossibleTypes.List;
            for (int i = 0; i < list.Count; ++i)
            {
                var unionType = ConvertTypeReference(type, list[i]) as IObjectGraphType;

                if (type.ResolveType == null && unionType != null && unionType.IsTypeOf == null)
                {
                    throw new InvalidOperationException(
                       $"Union type '{type.Name}' does not provide a 'resolveType' function " +
                       $"and possible Type '{unionType.Name}' does not provide a 'isTypeOf' function. " +
                        "There is no way to resolve this possible type during execution.");
                }

                list[i] = unionType!;
            }
        }
    }

    /// <summary>
    /// Converts a GraphQLTypeReference to the actual type from the dictionary.
    /// Recursively processes wrapper types (NonNull, List).
    /// </summary>
    private IGraphType ConvertTypeReference(INamedType parentType, IGraphType type)
    {
        if (type is NonNullGraphType nonNull)
        {
            nonNull.ResolvedType = ConvertTypeReference(parentType, nonNull.ResolvedType!);
            return nonNull;
        }

        if (type is ListGraphType list)
        {
            list.ResolvedType = ConvertTypeReference(parentType, list.ResolvedType!);
            return list;
        }

        if (type is GraphQLTypeReference reference)
        {
            if (_typeDictionary.TryGetValue(reference.TypeName, out var found))
                return found;

            if (_builtInTypes.TryGetValue(reference.TypeName, out var builtIn))
            {
                _typeDictionary[reference.TypeName] = builtIn;
                return builtIn;
            }

            throw new InvalidOperationException(
                    $"Unable to resolve reference to type '{reference.TypeName}' on '{parentType.Name}'");
        }

        return type;
    }
}
