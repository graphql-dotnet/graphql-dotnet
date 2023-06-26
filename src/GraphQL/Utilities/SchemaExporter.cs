using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities;

/// <summary>
/// Exports a schema definition to a SDL.
/// </summary>
public class SchemaExporter
{
    /// <summary>
    /// Returns the <see cref="ISchema"/> instance this exporter is exporting.
    /// </summary>
    protected ISchema Schema { get; }

    private static readonly HashSet<string> _builtInScalars = new()
    {
        "String",
        "Boolean",
        "Int",
        "Float",
        "ID"
    };

    private static readonly HashSet<string> _builtInDirectives = new()
    {
        "skip",
        "include",
        "deprecated"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaExporter"/> class for the specified <see cref="ISchema"/>.
    /// </summary>
    public SchemaExporter(ISchema schema)
    {
        Schema = schema;
    }

    /// <summary>
    /// Exports the schema as a <see cref="GraphQLDocument"/>.
    /// </summary>
    public virtual GraphQLDocument Export()
    {
        // initialize the schema, so all the ResolvedType properties are set
        Schema.Initialize();

        // export the schema definition
        var definitions = new List<ASTNode>
        {
            ExportSchemaDefinition()
        };

        // export directives
        foreach (var directive in Schema.Directives)
        {
            if (!IsBuiltInDirective(directive.Name))
                definitions.Add(ExportDirectiveDefinition(directive));
        }

        // export types
        foreach (var type in Schema.AllTypes)
        {
            if (!IsIntrospectionType(type.Name) && !IsBuiltInScalar(type.Name))
                definitions.Add(ExportTypeDefinition(type));
        }

        return new GraphQLDocument(definitions);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the specified type name is a built-in introspection type.
    /// </summary>
    protected static bool IsIntrospectionType(string typeName) => typeName.StartsWith("__", StringComparison.InvariantCulture);

    /// <summary>
    /// Returns <see langword="true"/> if the specified type name is a built-in scalar type.
    /// </summary>
    protected static bool IsBuiltInScalar(string typeName) => _builtInScalars.Contains(typeName);

    /// <summary>
    /// Returns <see langword="true"/> if the specified directive name is a built-in directive.
    /// </summary>
    protected static bool IsBuiltInDirective(string directiveName) => _builtInDirectives.Contains(directiveName);

    /// <summary>
    /// Exports the specified <see cref="IGraphType"/>.
    /// </summary>
    protected virtual GraphQLTypeDefinition ExportTypeDefinition(IGraphType graphType) => graphType switch
    {
        EnumerationGraphType enumType => ExportEnumTypeDefinition(enumType),
        ScalarGraphType scalarType => ExportScalarTypeDefinition(scalarType),
        IInterfaceGraphType interfaceType => ExportInterfaceTypeDefinition(interfaceType),
        IInputObjectGraphType inputType => ExportInputObjectTypeDefinition(inputType),
        UnionGraphType unionType => ExportUnionTypeDefinition(unionType),
        IObjectGraphType objectType => ExportObjectTypeDefinition(objectType),
        _ => throw new ArgumentOutOfRangeException(nameof(graphType), "Could not identify the type of graph type supplied.")
    };

    /// <summary>
    /// Exports the specified <see cref="IInputObjectGraphType"/>.
    /// </summary>
    protected virtual GraphQLInputObjectTypeDefinition ExportInputObjectTypeDefinition(IInputObjectGraphType graphType)
    {
        GraphQLInputFieldsDefinition? fields = null;
        if (graphType.Fields.Count > 0)
        {
            var list = new List<GraphQLInputValueDefinition>(graphType.Fields.Count);
            foreach (var field in graphType.Fields)
            {
                list.Add(ExportInputValueDefinition(field));
            }
            fields = new(list);
        }
        var ret = new GraphQLInputObjectTypeDefinition(new(graphType.Name))
        {
            Fields = fields,
        };
        return ApplyDescription(ApplyDirectives(ret, graphType), graphType);
    }

    /// <summary>
    /// Exports the specified <see cref="FieldType"/> as an input object's field definition.
    /// </summary>
    protected virtual GraphQLInputValueDefinition ExportInputValueDefinition(FieldType fieldType)
    {
        var ret = new GraphQLInputValueDefinition(new(fieldType.Name), ExportTypeReference(fieldType.ResolvedType!))
        {
            DefaultValue = fieldType.DefaultValue == null
                ? null
                : fieldType.ResolvedType!.ToAST(fieldType.DefaultValue),
        };
        return ApplyDescription(ApplyDirectives(ret, fieldType), fieldType);
    }

    /// <summary>
    /// Exports the specified <see cref="IInterfaceGraphType"/>.
    /// </summary>
    protected virtual GraphQLInterfaceTypeDefinition ExportInterfaceTypeDefinition(IInterfaceGraphType graphType)
    {
        // note: interface inheritance is not yet supported by GraphQL.NET
        GraphQLFieldsDefinition? fields = null;
        if (graphType.Fields.Count > 0)
        {
            var list = new List<GraphQLFieldDefinition>(graphType.Fields.Count);
            foreach (var field in graphType.Fields)
            {
                list.Add(ExportFieldDefinition(field));
            }
            fields = new(list);
        }
        var ret = new GraphQLInterfaceTypeDefinition(new(graphType.Name))
        {
            Fields = fields,
        };
        return ApplyDescription(ApplyDirectives(ret, graphType), graphType);
    }

    /// <summary>
    /// Exports the specified <see cref="IObjectGraphType"/>.
    /// </summary>
    protected virtual GraphQLObjectTypeDefinition ExportObjectTypeDefinition(IObjectGraphType graphType)
    {
        GraphQLImplementsInterfaces? interfaces = null;
        if (graphType.ResolvedInterfaces.Count > 0)
        {
            var list = new List<GraphQLNamedType>(graphType.ResolvedInterfaces.Count);
            foreach (var interfaceType in graphType.ResolvedInterfaces)
            {
                list.Add(new(new(interfaceType.Name)));
            }
            interfaces = new(list);
        }
        GraphQLFieldsDefinition? fields = null;
        if (graphType.Fields.Count > 0)
        {
            var list = new List<GraphQLFieldDefinition>(graphType.Fields.Count);
            foreach (var field in graphType.Fields)
            {
                list.Add(ExportFieldDefinition(field));
            }
            fields = new(list);
        }
        var ret = new GraphQLObjectTypeDefinition(new(graphType.Name))
        {
            Interfaces = interfaces,
            Fields = fields,
        };
        return ApplyDescription(ApplyDirectives(ret, graphType), graphType);
    }

    /// <summary>
    /// Exports the specified <see cref="FieldType"/>.
    /// </summary>
    protected virtual GraphQLFieldDefinition ExportFieldDefinition(FieldType fieldType)
    {
        var ret = new GraphQLFieldDefinition(new(fieldType.Name), ExportTypeReference(fieldType.ResolvedType!))
        {
            Arguments = ExportArgumentsDefinition(fieldType.Arguments),
        };
        return ApplyDescription(ApplyDirectives(ret, fieldType), fieldType);
    }

    /// <summary>
    /// Exports a <see cref="UnionGraphType"/>.
    /// </summary>
    protected virtual GraphQLUnionTypeDefinition ExportUnionTypeDefinition(UnionGraphType graphType)
    {
        GraphQLUnionMemberTypes? memberTypes = null;
        if (graphType.PossibleTypes.Count > 0)
        {
            var types = new List<GraphQLNamedType>(graphType.PossibleTypes.Count);
            foreach (var type in graphType.PossibleTypes)
            {
                types.Add(new(new(type.Name)));
            }
            memberTypes = new GraphQLUnionMemberTypes(types);
        }
        var unionDef = new GraphQLUnionTypeDefinition(new(graphType.Name))
        {
            Types = memberTypes,
        };
        return ApplyDescription(ApplyDirectives(unionDef, graphType), graphType);
    }

    /// <summary>
    /// Exports the specified <see cref="ScalarGraphType"/>.
    /// </summary>
    protected virtual GraphQLScalarTypeDefinition ExportScalarTypeDefinition(ScalarGraphType scalarType)
    {
        var scalarDef = new GraphQLScalarTypeDefinition(new(scalarType.Name));
        return ApplyDescription(ApplyDirectives(scalarDef, scalarType), scalarType);
    }

    /// <summary>
    /// Exports the specified <see cref="EnumerationGraphType"/>.
    /// </summary>
    protected virtual GraphQLEnumTypeDefinition ExportEnumTypeDefinition(EnumerationGraphType enumType)
    {
        GraphQLEnumValuesDefinition? valuesDef = null;
        if (enumType.Values.Count > 0)
        {
            var values = new List<GraphQLEnumValueDefinition>(enumType.Values.Count);
            foreach (var value in enumType.Values)
            {
                var name = new GraphQLName(value.Name);
                var def = new GraphQLEnumValueDefinition(
                    name,
                    new GraphQLEnumValue(name));
                values.Add(ApplyDescription(ApplyDirectives(def, value), value));
            }
            valuesDef = new GraphQLEnumValuesDefinition(values);
        }
        var enumDef = new GraphQLEnumTypeDefinition(new(enumType.Name))
        {
            Values = valuesDef,
        };
        return ApplyDescription(ApplyDirectives(enumDef, enumType), enumType);
    }

    /// <summary>
    /// Exports the schema definition.
    /// </summary>
    protected virtual GraphQLSchemaDefinition ExportSchemaDefinition()
    {
        var definitions = new List<GraphQLRootOperationTypeDefinition> {
            new GraphQLRootOperationTypeDefinition()
            {
                Operation = OperationType.Query,
                Type = new GraphQLNamedType(new(Schema.Query.Name))
            }
        };
        if (Schema.Mutation != null)
        {
            definitions.Add(new GraphQLRootOperationTypeDefinition()
            {
                Operation = OperationType.Mutation,
                Type = new GraphQLNamedType(new(Schema.Mutation.Name))
            });
        }
        if (Schema.Subscription != null)
        {
            definitions.Add(new GraphQLRootOperationTypeDefinition()
            {
                Operation = OperationType.Subscription,
                Type = new GraphQLNamedType(new(Schema.Subscription.Name))
            });
        }
        return ApplyDescription(ApplyDirectives(new GraphQLSchemaDefinition(definitions), Schema), Schema);
    }

    /// <summary>
    /// Exports the specified <see cref="Directive"/> definition.
    /// </summary>
    protected virtual GraphQLDirectiveDefinition ExportDirectiveDefinition(Directive directive)
    {
        var def = new GraphQLDirectiveDefinition(
            new(directive.Name),
            new GraphQLDirectiveLocations(directive.Locations))
        {
            Repeatable = directive.Repeatable,
            Arguments = ExportArgumentsDefinition(directive.Arguments),
        };
        return ApplyDescription(def, directive);
    }

    private GraphQLArgumentsDefinition? ExportArgumentsDefinition(QueryArguments? arguments)
    {
        if (arguments == null || arguments.Count == 0)
            return null;

        var args = new List<GraphQLInputValueDefinition>(arguments.Count);
        foreach (var arg in arguments)
        {
            args.Add(ExportArgumentDefinition(arg));
        }
        return new GraphQLArgumentsDefinition(args);
    }

    /// <summary>
    /// Exports the specified <see cref="QueryArgument"/> definition as a <see cref="GraphQLInputValueDefinition"/>.
    /// </summary>
    protected virtual GraphQLInputValueDefinition ExportArgumentDefinition(QueryArgument argument)
    {
        var defaultValue = argument.DefaultValue != null
            ? argument.ResolvedType!.ToAST(argument.DefaultValue)
            : null;

        var def = new GraphQLInputValueDefinition(
            new(argument.Name),
            ExportTypeReference(argument.ResolvedType!))
        {
            DefaultValue = defaultValue,
        };
        return ApplyDescription(ApplyDirectives(def, argument), argument);
    }

    /// <summary>
    /// Exports the specified <see cref="IGraphType"/> as a <see cref="GraphQLType"/> reference.
    /// </summary>
    protected virtual GraphQLType ExportTypeReference(IGraphType graphType)
    {
        if (graphType is NonNullGraphType nonNullGraphType)
        {
            return new GraphQLNonNullType(ExportTypeReference(nonNullGraphType.ResolvedType!));
        }
        else if (graphType is ListGraphType listGraphType)
        {
            return new GraphQLListType(ExportTypeReference(listGraphType.ResolvedType!));
        }
        else
        {
            return new GraphQLNamedType(new(graphType.Name));
        }
    }

    /// <summary>
    /// Adds a description to an AST node if the schema object has a description.
    /// </summary>
    protected virtual T ApplyDescription<T>(T node, IProvideDescription obj)
        where T : IHasDescriptionNode
    {
        if (!string.IsNullOrEmpty(obj.Description))
        {
            node.Description = new GraphQLDescription(obj.Description);
        }
        return node;
    }

    /// <summary>
    /// Adds any applied directives from a schema object to an AST node.
    /// If the schema object implements <see cref="IProvideDeprecationReason"/>
    /// and has a deprecation reason set, and if the @deprecated directive is not
    /// already set on the schema object, then the deprecation reason is added
    /// as a directive also.
    /// </summary>
    protected virtual T ApplyDirectives<T>(T node, IProvideMetadata obj) // v8: IMetadataReader
        where T : IHasDirectivesNode
    {
        var deprecationReason = (obj as IProvideDeprecationReason)?.DeprecationReason;
        var appliedDirectives = obj.GetAppliedDirectives();
        List<GraphQLDirective>? directives = null;
        if (appliedDirectives != null && appliedDirectives.Count > 0)
        {
            directives = new(appliedDirectives.Count + (deprecationReason != null ? 1 : 0));
            foreach (var appliedDirective in appliedDirectives)
            {
                directives.Add(ExportDirective(appliedDirective));
                // do not add the @deprecated directive twice; give preference to the directive
                // set within the metadata rather than the DeprecationReason property
                if (appliedDirective.Name == "deprecated")
                    deprecationReason = null;
            }
        }
        if (deprecationReason != null)
        {
            directives ??= new(1);
            directives.Add(new(new("deprecated"))
            {
                Arguments = deprecationReason == "" ? null :
                    new(new(1) { new(new("reason"), new GraphQLStringValue(deprecationReason)) })
            });
        }
        node.Directives = directives != null ? new GraphQLDirectives(directives) : null;
        return node;
    }

    /// <summary>
    /// Exports the specified <see cref="AppliedDirective"/>.
    /// </summary>
    protected virtual GraphQLDirective ExportDirective(AppliedDirective appliedDirective)
    {
        var directive = Schema.Directives.Find(appliedDirective.Name)
            ?? throw new InvalidOperationException($"Could not find an applied directive named '{appliedDirective.Name}' within the schema.");
        var ret = new GraphQLDirective(new(appliedDirective.Name));
        if (appliedDirective.ArgumentsCount > 0)
        {
            var arguments = new List<GraphQLArgument>(appliedDirective.ArgumentsCount);
            foreach (var argument in appliedDirective)
            {
                arguments.Add(ExportDirectiveArgument(directive, argument));
            }
            ret.Arguments = new GraphQLArguments(arguments);
        }
        return ret;
    }

    /// <summary>
    /// Exports the specified <see cref="DirectiveArgument"/>.
    /// </summary>
    protected virtual GraphQLArgument ExportDirectiveArgument(Directive directive, DirectiveArgument argument)
    {
        var directiveArgument = directive.Arguments?.Find(argument.Name)
            ?? throw new InvalidOperationException($"Unable to find argument '{argument.Name}' on directive '{directive.Name}'.");
        return new GraphQLArgument(new(argument.Name), directiveArgument.ResolvedType!.ToAST(argument.Value));
    }
}
