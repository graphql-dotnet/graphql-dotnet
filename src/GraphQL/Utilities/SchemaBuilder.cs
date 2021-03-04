using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using GraphQL.Introspection;
using GraphQL.Types;
using GraphQLParser;
using GraphQLParser.AST;
using OperationType = GraphQLParser.AST.OperationType;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Builds schema from string.
    /// </summary>
    public class SchemaBuilder
    {
        protected readonly Dictionary<string, IGraphType> _types = new Dictionary<string, IGraphType>();
        private GraphQLSchemaDefinition _schemaDef;

        /// <summary>
        /// This <see cref="IServiceProvider"/> is used to create required objects during building schema.
        /// <br/><br/>
        /// By default equals to <see cref="DefaultServiceProvider"/>.
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; } = new DefaultServiceProvider();

        /// <summary>
        /// Specifies whether to ignore comments when parsing GraphQL document.
        /// By default, all comments are ignored
        /// </summary>
        public bool IgnoreComments { get; set; } = true;

        /// <summary>
        /// Allows to successfully build the schema even if types are found that are not registered int <see cref="Types"/>.
        /// <br/>
        /// By default <see langword="true"/>.
        /// </summary>
        public bool AllowUnknownTypes { get; set; } = true;

        /// <summary>
        /// Allows to successfully build the schema even if fields are found that have no resolvers.
        /// <br/>
        /// By default <see langword="true"/>.
        /// </summary>
        public bool AllowUnknownFields { get; set; } = true;

        public TypeSettings Types { get; } = new TypeSettings();

        /// <summary>
        /// Builds schema from string.
        /// </summary>
        /// <param name="typeDefinitions">A textual description of the schema in SDL (Schema Definition Language) format.</param>
        /// <returns>Created schema.</returns>
        public virtual Schema Build(string typeDefinitions)
        {
            var document = Parser.Parse(typeDefinitions, new ParserOptions { Ignore = IgnoreComments ? IgnoreOptions.IgnoreComments : IgnoreOptions.None });
            Validate(document);
            return BuildSchemaFrom(document);
        }

        protected virtual void Validate(GraphQLDocument document)
        {
            var definitionsByName = document.Definitions.OfType<GraphQLTypeDefinition>().Where(def => !(def is GraphQLTypeExtensionDefinition)).ToLookup(def => def.Name.Value);
            var duplicates = definitionsByName.Where(grouping => grouping.Count() > 1).ToArray();
            if (duplicates.Length > 0)
            {
                throw new ArgumentException(@$"All types within a GraphQL schema must have unique names. No two provided types may have the same name.
Schema contains a redefinition of these types: {string.Join(", ", duplicates.Select(item => item.Key))}", nameof(document));
            }

            //TODO: checks for parsed SDL may be expanded in the future, see https://github.com/graphql/graphql-spec/issues/653
            // Also see Schema.Validate
        }

        private Schema BuildSchemaFrom(GraphQLDocument document)
        {
            var schema = new Schema(ServiceProvider);

            PreConfigure(schema);

            var directives = new List<DirectiveGraphType>();

            foreach (var def in document.Definitions)
            {
                switch (def.Kind)
                {
                    case ASTNodeKind.SchemaDefinition:
                    {
                        _schemaDef = def.As<GraphQLSchemaDefinition>();
                        schema.SetAstType(_schemaDef);
                        break;
                    }

                    case ASTNodeKind.ObjectTypeDefinition:
                    {
                        var type = ToObjectGraphType(def.As<GraphQLObjectTypeDefinition>());
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.TypeExtensionDefinition:
                    {
                        var type = ToObjectGraphType(def.As<GraphQLTypeExtensionDefinition>().Definition, true);
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.InterfaceTypeDefinition:
                    {
                        var type = ToInterfaceType(def.As<GraphQLInterfaceTypeDefinition>());
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.EnumTypeDefinition:
                    {
                        var type = ToEnumerationType(def.As<GraphQLEnumTypeDefinition>());
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.UnionTypeDefinition:
                    {
                        var type = ToUnionType(def.As<GraphQLUnionTypeDefinition>());
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.InputObjectTypeDefinition:
                    {
                        var type = ToInputObjectType(def.As<GraphQLInputObjectTypeDefinition>());
                        _types[type.Name] = type;
                        break;
                    }

                    case ASTNodeKind.DirectiveDefinition:
                    {
                        var directive = ToDirective(def.As<GraphQLDirectiveDefinition>());
                        directives.Add(directive);
                        break;
                    }
                }
            }

            if (_schemaDef != null)
            {
                schema.Description = _schemaDef.Comment?.Text.ToString();

                foreach (var operationTypeDef in _schemaDef.OperationTypes)
                {
                    var typeName = (string)operationTypeDef.Type.Name.Value;
                    var type = GetType(typeName) as IObjectGraphType;

                    switch (operationTypeDef.Operation)
                    {
                        case OperationType.Query:
                            schema.Query = type;
                            break;

                        case OperationType.Mutation:
                            schema.Mutation = type;
                            break;

                        case OperationType.Subscription:
                            schema.Subscription = type;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException($"Unknown operation type {operationTypeDef.Operation}");
                    }
                }
            }
            else
            {
                schema.Query = GetType("Query") as IObjectGraphType;
                schema.Mutation = GetType("Mutation") as IObjectGraphType;
                schema.Subscription = GetType("Subscription") as IObjectGraphType;
            }

            foreach (var type in _types)
                schema.RegisterType(type.Value);

            foreach (var directive in directives)
                schema.Directives.Register(directive);

            Debug.Assert(schema.Initialized == false);
            return schema;
        }

        protected virtual void PreConfigure(Schema schema)
        {
        }

        protected virtual IGraphType GetType(string name)
        {
            _types.TryGetValue(name, out IGraphType type);
            return type;
        }

        private bool IsSubscriptionType(ObjectGraphType type)
        {
            var operationDefinition = _schemaDef?.OperationTypes?.FirstOrDefault(o => o.Operation == OperationType.Subscription);
            if (operationDefinition == null)
                return type.Name == "Subscription";

            return type.Name == operationDefinition.Type.Name.Value;
        }

        private void AssertKnownType(TypeConfig typeConfig)
        {
            if (typeConfig.Type == null && !AllowUnknownTypes) //TODO: the same for subscriptions?
                throw new InvalidOperationException($"Unknown type '{typeConfig.Name}'. Verify that you have configured SchemaBuilder correctly.");
        }

        private void AssertKnownField(TypeConfig typeConfig, FieldConfig fieldConfig)
        {
            if (fieldConfig.Resolver == null && !AllowUnknownFields) //TODO: the same for subscriptions?
                throw new InvalidOperationException($"Unknown field '{typeConfig.Name}.{fieldConfig.Name}' has no resolver. Verify that you have configured SchemaBuilder correctly.");
        }

        protected virtual IObjectGraphType ToObjectGraphType(GraphQLObjectTypeDefinition astType, bool isExtensionType = false)
        {
            var name = (string)astType.Name.Value;
            var typeConfig = Types.For(name);

            AssertKnownType(typeConfig);

            ObjectGraphType type;
            if (!_types.ContainsKey(name))
            {
                type = new ObjectGraphType { Name = name };
            }
            else
            {
                type = _types[name] as ObjectGraphType ?? throw new InvalidOperationException($"Type '{name} should be ObjectGraphType");
            }

            if (!isExtensionType)
            {
                type.Description = typeConfig.Description ?? astType.Comment?.Text.ToString();
                type.DeprecationReason = typeConfig.DeprecationReason;
                type.IsTypeOf = typeConfig.IsTypeOfFunc;
            }

            typeConfig.CopyMetadataTo(type);

            Func<string, GraphQLFieldDefinition, FieldType> constructFieldType;
            if (IsSubscriptionType(type))
            {
                constructFieldType = ToSubscriptionFieldType;
            }
            else
            {
                constructFieldType = ToFieldType;
            }

            if (astType.Fields != null)
            {
                foreach (var f in astType.Fields)
                    type.AddField(constructFieldType(type.Name, f));
            }

            if (astType.Interfaces != null)
            {
                foreach (var i in astType.Interfaces)
                    type.AddResolvedInterface(new GraphQLTypeReference((string)i.Name.Value));
            }

            if (isExtensionType)
            {
                type.AddExtensionAstType(astType);
            }
            else
            {
                type.SetAstType(astType);
            }

            return type;
        }

        protected virtual FieldType ToFieldType(string parentTypeName, GraphQLFieldDefinition fieldDef)
        {
            var typeConfig = Types.For(parentTypeName);

            AssertKnownType(typeConfig);

            var name = (string)fieldDef.Name.Value;
            var fieldConfig = typeConfig.FieldFor(name, ServiceProvider);

            AssertKnownField(typeConfig, fieldConfig);

            var field = new FieldType
            {
                Name = name,
                Description = fieldConfig.Description ?? fieldDef.Comment?.Text.ToString(),
                DeprecationReason = fieldConfig.DeprecationReason,
                ResolvedType = ToGraphType(fieldDef.Type),
                Resolver = fieldConfig.Resolver
            };

            fieldConfig.CopyMetadataTo(field);

            field.Arguments = ToQueryArguments(fieldDef.Arguments);

            field.SetAstType(fieldDef);

            return field;
        }

        protected virtual FieldType ToSubscriptionFieldType(string parentTypeName, GraphQLFieldDefinition fieldDef)
        {
            var typeConfig = Types.For(parentTypeName);

            AssertKnownType(typeConfig);

            var name = (string)fieldDef.Name.Value;
            var fieldConfig = typeConfig.SubscriptionFieldFor(name, ServiceProvider);

            var field = new EventStreamFieldType
            {
                Name = name,
                Description = fieldConfig.Description ?? fieldDef.Comment?.Text.ToString(),
                DeprecationReason = fieldConfig.DeprecationReason,
                ResolvedType = ToGraphType(fieldDef.Type),
                Resolver = fieldConfig.Resolver,
                Subscriber = fieldConfig.Subscriber,
                AsyncSubscriber = fieldConfig.AsyncSubscriber,
            };

            fieldConfig.CopyMetadataTo(field);

            field.Arguments = ToQueryArguments(fieldDef.Arguments);

            field.SetAstType(fieldDef);

            return field;
        }

        protected virtual FieldType ToFieldType(string parentTypeName, GraphQLInputValueDefinition inputDef)
        {
            var typeConfig = Types.For(parentTypeName);

            AssertKnownType(typeConfig);

            var name = (string)inputDef.Name.Value;
            var fieldConfig = typeConfig.FieldFor(name, ServiceProvider);

            var field = new FieldType
            {
                Name = name,
                Description = fieldConfig.Description ?? inputDef.Comment?.Text.ToString(),
                DeprecationReason = fieldConfig.DeprecationReason,
                ResolvedType = ToGraphType(inputDef.Type),
                DefaultValue = inputDef.DefaultValue
            }.SetAstType(inputDef);

            return field;
        }

        protected virtual InterfaceGraphType ToInterfaceType(GraphQLInterfaceTypeDefinition interfaceDef)
        {
            var name = (string)interfaceDef.Name.Value;
            var typeConfig = Types.For(name);

            AssertKnownType(typeConfig);

            var type = new InterfaceGraphType
            {
                Name = name,
                Description = typeConfig.Description ?? interfaceDef.Comment?.Text.ToString(),
                DeprecationReason = typeConfig.DeprecationReason,
                ResolveType = typeConfig.ResolveType,
            }.SetAstType(interfaceDef);

            typeConfig.CopyMetadataTo(type);

            if (interfaceDef.Fields != null)
            {
                foreach (var f in interfaceDef.Fields)
                    type.AddField(ToFieldType(type.Name, f));
            }

            return type;
        }

        protected virtual UnionGraphType ToUnionType(GraphQLUnionTypeDefinition unionDef)
        {
            var name = (string)unionDef.Name.Value;
            var typeConfig = Types.For(name);

            AssertKnownType(typeConfig);

            var type = new UnionGraphType
            {
                Name = name,
                Description = typeConfig.Description ?? unionDef.Comment?.Text.ToString(),
                DeprecationReason = typeConfig.DeprecationReason,
                ResolveType = typeConfig.ResolveType,
            }.SetAstType(unionDef);

            typeConfig.CopyMetadataTo(type);

            if (unionDef.Types?.Count > 0) // just in case
            {
                foreach (var x in unionDef.Types)
                {
                    string n = (string)x.Name.Value;
                    type.AddPossibleType((GetType(n) ?? new GraphQLTypeReference(n)) as IObjectGraphType);
                }
            }

            return type;
        }

        protected virtual InputObjectGraphType ToInputObjectType(GraphQLInputObjectTypeDefinition inputDef)
        {
            var name = (string)inputDef.Name.Value;
            var typeConfig = Types.For(name);

            AssertKnownType(typeConfig);

            var type = new InputObjectGraphType
            {
                Name = name,
                Description = typeConfig.Description ?? inputDef.Comment?.Text.ToString(),
                DeprecationReason = typeConfig.DeprecationReason,
            }.SetAstType(inputDef);

            typeConfig.CopyMetadataTo(type);

            if (inputDef.Fields != null)
            {
                foreach (var f in inputDef.Fields)
                    type.AddField(ToFieldType(type.Name, f));
            }

            return type;
        }

        protected virtual EnumerationGraphType ToEnumerationType(GraphQLEnumTypeDefinition enumDef)
        {
            var name = (string)enumDef.Name.Value;
            var typeConfig = Types.For(name);

            AssertKnownType(typeConfig);

            var type = new EnumerationGraphType
            {
                Name = name,
                Description = typeConfig.Description ?? enumDef.Comment?.Text.ToString(),
                DeprecationReason = typeConfig.DeprecationReason,
            }.SetAstType(enumDef);

            if (enumDef.Values?.Count > 0) // just in case
            {
                foreach (var value in enumDef.Values)
                    type.AddValue(ToEnumValue(value));
            }

            return type;
        }

        protected virtual DirectiveGraphType ToDirective(GraphQLDirectiveDefinition directiveDef)
        {
            var result = new DirectiveGraphType((string)directiveDef.Name.Value)
            {
                Description = directiveDef.Comment?.Text.ToString(),
                Repeatable = directiveDef.Repeatable,
                Arguments = ToQueryArguments(directiveDef.Arguments)
            };

            if (directiveDef.Locations?.Count > 0) // just in case
            {
                foreach (var location in directiveDef.Locations)
                {
                    if (__DirectiveLocation.Instance.Values.FindByName(location.Value)?.Value is DirectiveLocation l)
                        result.Locations.Add(l);
                    else
                        throw new InvalidOperationException($"Directive '{result.Name}' has an unknown directive location '{location.Value}'.");
                }
            }

            return result;
        }

        private EnumValueDefinition ToEnumValue(GraphQLEnumValueDefinition valDef)
        {
            var name = (string)valDef.Name.Value;
            return new EnumValueDefinition
            {
                Value = name,
                Name = name,
                Description = valDef.Comment?.Text.ToString()
            }.SetAstType(valDef);
        }

        protected virtual QueryArgument ToArgument(GraphQLInputValueDefinition inputDef)
        {
            return new QueryArgument(ToGraphType(inputDef.Type))
            {
                Name = (string)inputDef.Name.Value,
                DefaultValue = inputDef.DefaultValue,
                Description = inputDef.Comment?.Text.ToString()
            }.SetAstType(inputDef);
        }

        private IGraphType ToGraphType(GraphQLType astType)
        {
            switch (astType.Kind)
            {
                case ASTNodeKind.NonNullType:
                {
                    var type = ToGraphType(((GraphQLNonNullType)astType).Type);
                    return new NonNullGraphType(type);
                }

                case ASTNodeKind.ListType:
                {
                    var type = ToGraphType(((GraphQLListType)astType).Type);
                    return new ListGraphType(type);
                }

                case ASTNodeKind.NamedType:
                {
                    var namedType = (GraphQLNamedType)astType;
                    var name = (string)namedType.Name.Value;
                    var type = GetType(name);
                    return type ?? new GraphQLTypeReference(name);
                }

                default:
                    throw new ArgumentOutOfRangeException($"Unknown GraphQL type {astType.Kind}");
            }
        }

        private QueryArguments ToQueryArguments(List<GraphQLInputValueDefinition> arguments)
        {
            return arguments == null ? new QueryArguments() : new QueryArguments(arguments.Select(ToArgument));
        }
    }

    internal static class Extensions
    {
        public static TNode As<TNode>(this ASTNode node) where TNode : ASTNode
        {
            return node as TNode ?? throw new InvalidOperationException($"Node should be of type '{typeof(TNode).Name}' but it is of type '{node?.GetType().Name}'.");
        }

        public static object ToValue(this GraphQLValue source)
        {
            if (source == null)
            {
                return null;
            }

            switch (source.Kind)
            {
                case ASTNodeKind.NullValue:
                {
                    return null;
                }
                case ASTNodeKind.StringValue:
                {
                    var str = source as GraphQLScalarValue;
                    Debug.Assert(str != null, nameof(str) + " != null");
                    return (string)str.Value;
                }
                case ASTNodeKind.IntValue:
                {
                    var str = source as GraphQLScalarValue;

                    Debug.Assert(str != null, nameof(str) + " != null");
                    if (Int.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int intResult))
                    {
                        return intResult;
                    }

                    // If the value doesn't fit in an integer, revert to using long...
                    if (Long.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long longResult))
                    {
                        return longResult;
                    }

                    // If the value doesn't fit in an long, revert to using decimal...
                    if (Decimal.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal decimalResult))
                    {
                        return decimalResult;
                    }

                    // If the value doesn't fit in an decimal, revert to using BigInteger...
                    if (BigInt.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var bigIntegerResult))
                    {
                        return bigIntegerResult;
                    }

                    throw new InvalidOperationException($"Invalid number {str.Value}");
                }
                case ASTNodeKind.FloatValue:
                {
                    var str = source as GraphQLScalarValue;
                    Debug.Assert(str != null, nameof(str) + " != null");

                    // the idea is to see if there is a loss of accuracy of value
                    // for example, 12.1 or 12.11 is double but 12.10 is decimal
                    if (Double.TryParse(
                        str.Value,
                        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                        CultureInfo.InvariantCulture,
                        out double dbl) == false)
                    {
                        dbl = str.Value.Span[0] == '-' ? double.NegativeInfinity : double.PositiveInfinity;
                    }

                    //it is possible for a FloatValue to overflow a decimal; however, with a double, it just returns Infinity or -Infinity
                    if (Decimal.TryParse(
                        str.Value,
                        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                        CultureInfo.InvariantCulture,
                        out decimal dec))
                    {
                        // Cast the decimal to our struct to avoid the decimal.GetBits allocations.
                        var decBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref dec);
                        decimal temp = new decimal(dbl);
                        var dblAsDecBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref temp);
                        if (!decBits.Equals(dblAsDecBits))
                            return dec;
                    }

                    return dbl;
                }
                case ASTNodeKind.BooleanValue:
                {
                    var str = source as GraphQLScalarValue;
                    Debug.Assert(str != null, nameof(str) + " != null");
                    return (str.Value.Length == 4).Boxed(); /*true.Length=4*/
                }
                case ASTNodeKind.EnumValue:
                {
                    var str = source as GraphQLScalarValue;
                    Debug.Assert(str != null, nameof(str) + " != null");
                    return (string)str.Value;
                }
                case ASTNodeKind.ObjectValue:
                {
                    var obj = source as GraphQLObjectValue;
                    var values = new Dictionary<string, object>();

                    Debug.Assert(obj != null, nameof(obj) + " != null");
                    if (obj.Fields != null)
                    {
                        foreach (var f in obj.Fields)
                            values[(string)f.Name.Value] = ToValue(f.Value);
                    }

                    return values;
                }
                case ASTNodeKind.ListValue:
                {
                    var list = source as GraphQLListValue;
                    Debug.Assert(list != null, nameof(list) + " != null");

                    if (list.Values == null)
                        return Array.Empty<object>();

                    object[] values = list.Values.Select(ToValue).ToArray();
                    return values;
                }
                default:
                    throw new InvalidOperationException($"Unsupported value type {source.Kind}");
            }
        }
    }
}
