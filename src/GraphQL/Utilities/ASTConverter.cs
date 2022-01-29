using System;
using System.Collections.Generic;
using GraphQL.Introspection;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Converts <see cref="ISchema"/> into <see cref="GraphQLDocument"/>.
    /// </summary>
    public class ASTConverter
    {
        /// <summary>
        /// Creates converter with the specified options.
        /// </summary>
        /// <param name="options">Converter options.</param>
        public ASTConverter(ASTConverterOptions? options = null)
        {
            Options = options ?? new ASTConverterOptions();
        }

        /// <summary>
        /// Converter options.
        /// </summary>
        public ASTConverterOptions Options { get; }

        /**
         * GraphQL schema define root types for each type of operation. These types are
         * the same as any other type and can be named in any manner, however there is
         * a common naming convention:
         *
         *   schema {
         *     query: Query
         *     mutation: Mutation
         *     subscription: Subscription
         *   }
         *
         * When using this naming convention, the schema definition can be omitted.
         */
        protected static bool IsSchemaOfCommonNames(ISchema schema)
        {
            if (schema.Query != null && schema.Query.Name != "Query")
                return false;

            if (schema.Mutation != null && schema.Mutation.Name != "Mutation")
                return false;

            if (schema.Subscription != null && schema.Subscription.Name != "Subscription")
                return false;

            return true;
        }

        /// <inheritdoc cref="ASTConverter"/>
        public virtual GraphQLDocument Convert(ISchema schema)
        {
            schema.Initialize();

            var document = new GraphQLDocument
            {
                Definitions = new List<ASTNode>(schema.AllTypes.Count + schema.Directives.Count + 1)
            };

            var schemaDef = ConvertSchemaDefinition(schema);
            if (schemaDef != null)
                document.Definitions.Add(schemaDef);

            foreach (var directive in schema.Directives.OrderBy(Options.Comparer?.DirectiveComparer))
            {
                var def = ConvertDirectiveDefinition(directive, schema);
                document.Definitions.Add(def);
            }

            foreach (var type in schema.AllTypes.OrderBy(Options.Comparer?.TypeComparer))
            {
                var def = ConvertTypeDefinition(type, schema);
                document.Definitions.Add(def);
            }

            return document;
        }

        public virtual GraphQLSchemaDefinition? ConvertSchemaDefinition(ISchema schema)
        {
            return IsSchemaOfCommonNames(schema)
                ? null
                : new GraphQLSchemaDefinition
                {
                    Description = ConvertDescription(schema), //TODO: what about description without custom query/mutation/subscription
                    OperationTypes = ConvertRootOperationTypeDefinitions(schema)
                };
        }

        protected virtual GraphQLValue? ConvertValue(object? value, IGraphType type)
        {
            //TODO: what about initial null in default ?
            return value == null ? null : type.ToAST(value);
        }

        protected virtual GraphQLType ConvertType(IGraphType type)
        {
            return type switch
            {
                NonNullGraphType nonNull => new GraphQLNonNullType { Type = ConvertType(nonNull.ResolvedType!) },
                ListGraphType list => new GraphQLListType { Type = ConvertType(list.ResolvedType!) },
                _ => new GraphQLNamedType { Name = new GraphQLName(type.Name) }
            };
        }

        protected virtual List<GraphQLRootOperationTypeDefinition> ConvertRootOperationTypeDefinitions(ISchema schema)
        {
            var list = new List<GraphQLRootOperationTypeDefinition>(3);

            if (schema.Query != null)
            {
                list.Add(new GraphQLRootOperationTypeDefinition
                {
                    Operation = OperationType.Query,
                    Type = new GraphQLNamedType { Name = new GraphQLName(schema.Query.Name) }
                });
            }

            if (schema.Mutation != null)
            {
                list.Add(new GraphQLRootOperationTypeDefinition
                {
                    Operation = OperationType.Mutation,
                    Type = new GraphQLNamedType { Name = new GraphQLName(schema.Mutation.Name) }
                });
            }

            if (schema.Subscription != null)
            {
                list.Add(new GraphQLRootOperationTypeDefinition
                {
                    Operation = OperationType.Subscription,
                    Type = new GraphQLNamedType { Name = new GraphQLName(schema.Subscription.Name) }
                });
            }

            return list;
        }

        public virtual GraphQLTypeDefinition ConvertTypeDefinition(IGraphType type, ISchema schema)
        {
            return type switch
            {
                EnumerationGraphType enumeration => ConvertEnumTypeDefinition(enumeration, schema),
                ScalarGraphType scalar => ConvertScalarTypeDefinition(scalar, schema),
                IObjectGraphType obj => ConvertObjectTypeDefinition(obj, schema),
                IInterfaceGraphType iface => ConvertInterfaceTypeDefinition(iface, schema),
                UnionGraphType union => ConvertUnionTypeDefinition(union, schema),
                IInputObjectGraphType input => ConvertInputObjectTypeDefinition(input, schema),
                _ => throw new NotSupportedException($"Unknown GraphType '{type.GetType().Name}' with name '{type.Name}'")
            };
        }

        protected virtual GraphQLArgumentsDefinition? ConvertArgumentsDefinition(QueryArguments? arguments, IComparer<QueryArgument>? comparer, ISchema schema)
        {
            if (arguments == null || arguments.Count == 0)
                return null;

            var argumentsDef = new GraphQLArgumentsDefinition
            {
                Items = new List<GraphQLInputValueDefinition>(arguments.Count)
            };

            foreach (var arg in arguments.List!.OrderBy(comparer))
            {
                var argument = new GraphQLInputValueDefinition
                {
                    Description = ConvertDescription(arg),
                    Name = new GraphQLName(arg.Name),
                    Type = ConvertType(arg.ResolvedType!),
                    Directives = ConvertDirectives(arg, schema),
                    DefaultValue = ConvertValue(arg.DefaultValue, arg.ResolvedType!)
                };
                argumentsDef.Items.Add(argument);
            }

            return argumentsDef;
        }

        protected virtual GraphQLArguments? ConvertDirectiveArguments(AppliedDirective directive, DirectiveGraphType type)
        {
            if (directive.ArgumentsCount == 0)
                return null;

            var arguments = new GraphQLArguments
            {
                Items = new List<GraphQLArgument>(directive.ArgumentsCount)
            };

            foreach (var dirArg in directive.List!)
            {
                var arg = new GraphQLArgument
                {
                    Name = new GraphQLName(dirArg.Name),
                    Value = dirArg.Value == null ? GraphQLValuesCache.Null : ConvertValue(dirArg.Value, type.Arguments!.Find(dirArg.Name)!.ResolvedType!)! //TODO: ???
                };
                arguments.Items.Add(arg);
            }

            return arguments;
        }

        protected virtual GraphQLDirectives? ConvertDirectives(IProvideMetadata provider, ISchema schema)
        {
            var appliedDirectives = provider.GetAppliedDirectives();
            if (appliedDirectives == null || appliedDirectives.Count == 0)
                return null;

            var directives = new GraphQLDirectives
            {
                Items = new List<GraphQLDirective>(appliedDirectives.Count)
            };

            foreach (var applied in appliedDirectives)
            {
                var directive = schema.Directives.Find(applied.Name);
                var dir = new GraphQLDirective
                {
                    Name = new GraphQLName(applied.Name),
                    Arguments = ConvertDirectiveArguments(applied, directive!),
                };
                directives.Items.Add(dir);
            }

            return directives;
        }

        protected virtual GraphQLDirectiveLocations ConvertDirectiveLocations(DirectiveGraphType directive)
        {
            var locations = new GraphQLDirectiveLocations
            {
                Items = new List<DirectiveLocation>(directive.Locations.Count)
            };

            foreach (var location in directive.Locations)
                locations.Items.Add(location);

            return locations;
        }

        protected virtual GraphQLDirectiveDefinition ConvertDirectiveDefinition(DirectiveGraphType directive, ISchema schema)
        {
            return new GraphQLDirectiveDefinition
            {
                Description = ConvertDescription(directive),
                Name = new GraphQLName(directive.Name),
                Arguments = ConvertArgumentsDefinition(directive.Arguments, Options.Comparer?.ArgumentComparer(directive), schema),
                Repeatable = directive.Repeatable,
                Locations = ConvertDirectiveLocations(directive)
            };
        }

        protected virtual GraphQLDescription? ConvertDescription(IProvideDescription provider)
        {
            return provider.Description == null || !Options.IncludeDescriptions
                ? null
                : new GraphQLDescription(provider.Description);
        }

        protected virtual GraphQLEnumValuesDefinition? ConvertEnumValues(EnumerationGraphType type, ISchema schema)
        {
            if (type.Values.Count == 0)
                return null;

            var valuesDef = new GraphQLEnumValuesDefinition
            {
                Items = new List<GraphQLEnumValueDefinition>(type.Values.Count)
            };

            foreach (var enumValue in type.Values.List.OrderBy(Options.Comparer?.EnumValueComparer(type)))
            {
                var def = new GraphQLEnumValueDefinition
                {
                    Description = ConvertDescription(enumValue),
                    EnumValue = new GraphQLEnumValue { Name = new GraphQLName(enumValue.Name) },
                    Name = new GraphQLName(enumValue.Name),
                    Directives = ConvertDirectives(enumValue, schema)
                };
                valuesDef.Items.Add(def);
            }

            return valuesDef;
        }

        protected virtual GraphQLInputFieldsDefinition? CreateInputFieldsDefinition(IInputObjectGraphType type, ISchema schema)
        {
            if (type.Fields.Count == 0)
                return null;

            var fields = new GraphQLInputFieldsDefinition
            {
                Items = new List<GraphQLInputValueDefinition>(type.Fields.Count)
            };

            foreach (var field in type.Fields.List.OrderBy(Options.Comparer?.FieldComparer(type)))
            {
                var inputValueDef = new GraphQLInputValueDefinition
                {
                    Description = ConvertDescription(field),
                    Name = new GraphQLName(field.Name),
                    Type = ConvertType(field.ResolvedType!),
                    Directives = ConvertDirectives(field, schema),
                    DefaultValue = ConvertValue(field.DefaultValue, field.ResolvedType!)
                };
                fields.Items.Add(inputValueDef);
            }

            return fields;
        }

        protected virtual GraphQLFieldsDefinition? ConvertFieldsDefinition(IComplexGraphType type, ISchema schema)
        {
            if (type.Fields.Count == 0)
                return null;

            var fields = new GraphQLFieldsDefinition
            {
                Items = new List<GraphQLFieldDefinition>(type.Fields.Count)
            };

            foreach (var field in type.Fields.List.OrderBy(Options.Comparer?.FieldComparer(type)))
            {
                var fieldDef = new GraphQLFieldDefinition
                {
                    Description = ConvertDescription(field),
                    Name = new GraphQLName(field.Name),
                    Type = ConvertType(field.ResolvedType!),
                    Arguments = ConvertArgumentsDefinition(field.Arguments, Options.Comparer?.ArgumentComparer(field), schema),
                    Directives = ConvertDirectives(field, schema)

                };
                fields.Items.Add(fieldDef);
            }

            return fields;
        }

        protected virtual GraphQLImplementsInterfaces? ConvertImplementsInterfaces(IImplementInterfaces type)
        {
            if (type.ResolvedInterfaces.Count == 0)
                return null;

            var implementedInterfaces = new GraphQLImplementsInterfaces
            {
                Items = new List<GraphQLNamedType>(type.ResolvedInterfaces.Count)
            };

            //TODO: add comparer
            foreach (var iface in type.ResolvedInterfaces.List)//.OrderBy(Options.Comparer?.EnumValueComparer(type)))
            {
                implementedInterfaces.Items.Add(new GraphQLNamedType { Name = new GraphQLName(iface.Name) });
            }

            return implementedInterfaces;
        }

        protected virtual GraphQLUnionMemberTypes? ConvertUnionMemberTypes(UnionGraphType type)
        {
            if (type.PossibleTypes.Count == 0)
                return null;

            var memberTypes = new GraphQLUnionMemberTypes
            {
                Items = new List<GraphQLNamedType>(type.PossibleTypes.Count)
            };

            //TODO: add unionmemberscomparer
            foreach (var memberType in type.PossibleTypes.List)//.OrderBy(Options.Comparer?.EnumValueComparer(type)))
            {
                memberTypes.Items.Add(new GraphQLNamedType { Name = new GraphQLName(memberType.Name) });
            }

            return memberTypes;
        }

        protected virtual GraphQLInterfaceTypeDefinition ConvertInterfaceTypeDefinition(IInterfaceGraphType type, ISchema schema)
        {
            return new GraphQLInterfaceTypeDefinition
            {
                Description = ConvertDescription(type),
                Name = new GraphQLName(type.Name),
                Fields = ConvertFieldsDefinition(type, schema),
                Interfaces = null,//ConvertImplementsInterfaces(/*type*/null!), //TODO: interface CAN implement interfaces now
                Directives = ConvertDirectives(type, schema)
            };
        }

        protected virtual GraphQLEnumTypeDefinition ConvertEnumTypeDefinition(EnumerationGraphType type, ISchema schema)
        {
            return new GraphQLEnumTypeDefinition
            {
                Description = ConvertDescription(type),
                Name = new GraphQLName(type.Name),
                Values = ConvertEnumValues(type, schema),
                Directives = ConvertDirectives(type, schema)
            };
        }

        protected virtual GraphQLScalarTypeDefinition ConvertScalarTypeDefinition(ScalarGraphType type, ISchema schema)
        {
            return new GraphQLScalarTypeDefinition
            {
                Description = ConvertDescription(type),
                Name = new GraphQLName(type.Name),
                Directives = ConvertDirectives(type, schema)
            };
        }

        protected virtual GraphQLInputObjectTypeDefinition ConvertInputObjectTypeDefinition(IInputObjectGraphType type, ISchema schema)
        {
            return new GraphQLInputObjectTypeDefinition
            {
                Description = ConvertDescription(type),
                Name = new GraphQLName(type.Name),
                Fields = CreateInputFieldsDefinition(type, schema),
                Directives = ConvertDirectives(type, schema)
            };
        }

        protected virtual GraphQLObjectTypeDefinition ConvertObjectTypeDefinition(IObjectGraphType type, ISchema schema)
        {
            return new GraphQLObjectTypeDefinition
            {
                Description = ConvertDescription(type),
                Name = new GraphQLName(type.Name),
                Fields = ConvertFieldsDefinition(type, schema),
                Interfaces = ConvertImplementsInterfaces(type),
                Directives = ConvertDirectives(type, schema)
            };
        }

        protected virtual GraphQLUnionTypeDefinition ConvertUnionTypeDefinition(UnionGraphType type, ISchema schema)
        {
            return new GraphQLUnionTypeDefinition
            {
                Description = ConvertDescription(type),
                Name = new GraphQLName(type.Name),
                Types = ConvertUnionMemberTypes(type),
                Directives = ConvertDirectives(type, schema)
            };
        }
    }

    /// <summary>
    /// Options for converting <see cref="ISchema"/> into <see cref="GraphQLDocument"/>
    /// using <see cref="ASTConverter.Convert(ISchema)"/>.
    /// </summary>
    public class ASTConverterOptions
    {
        /// <summary>
        /// Indicates whether to include a description for types, fields, directives, arguments and other schema elements.
        /// </summary>
        public bool IncludeDescriptions { get; set; }

        /// <summary>
        /// Provides the ability to order the schema elements upon conversion.
        /// By default all elements are converted as-is; no sorting is applied.
        /// </summary>
        public ISchemaComparer? Comparer { get; set; }
    }
}
