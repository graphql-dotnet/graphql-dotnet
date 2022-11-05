using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// <see cref="__Type"/> is at the core of the type introspection system.
    /// It represents scalars, interfaces, object types, unions, enums in the system.
    /// </summary>
    public class __Type : ObjectGraphType<IGraphType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="__Type"/> introspection type.
        /// </summary>
        /// <param name="allowAppliedDirectives">Allows 'appliedDirectives' field for this type. It is an experimental feature.</param>
        public __Type(bool allowAppliedDirectives = false)
        {
            Name = nameof(__Type);

            Description =
                "The fundamental unit of any GraphQL Schema is the type. There are " +
                "many kinds of types in GraphQL as represented by the `__TypeKind` enum." +
               @"

" +
                "Depending on the kind of a type, certain fields describe " +
                "information about that type. Scalar types provide no information " +
                "beyond a name and description, while Enum types provide their values. " +
                "Object and Interface types provide the fields they describe. Abstract " +
                "types, Union and Interface, provide the Object types possible " +
                "at runtime. List and NonNull types compose other types.";

            Field<NonNullGraphType<__TypeKind>>("kind").Resolve(context =>
            {
                return context.Source is IGraphType type
                    ? KindForInstance(type)
                    : throw new InvalidOperationException($"Unknown kind of type: {context.Source}");
            });

            Field<StringGraphType>("name").Resolve(context => context.Source!.Name);

            Field<StringGraphType>("description");

            Field<ListGraphType<NonNullGraphType<__Field>>>("fields")
                .Argument<BooleanGraphType>("includeDeprecated", arg => arg.DefaultValue = BoolBox.False)
                .ResolveAsync(async context =>
                {
                    if (context.Source is IObjectGraphType || context.Source is IInterfaceGraphType)
                    {
                        var type = (IComplexGraphType)context.Source;
                        var fields = context.ArrayPool.Rent<FieldType>(type.Fields.Count);

                        bool includeDeprecated = context.GetArgument<bool>("includeDeprecated");

                        int index = 0;
                        foreach (var field in type.Fields.List)
                        {
                            if ((includeDeprecated || string.IsNullOrWhiteSpace(field.DeprecationReason)) && await context.Schema.Filter.AllowField(type, field).ConfigureAwait(false))
                                fields[index++] = field;
                        }

                        var comparer = context.Schema.Comparer.FieldComparer(type);
                        if (comparer != null)
                            Array.Sort(fields, 0, index, comparer);

                        return fields.Constrained(index);
                    }
                    return null;
                });

            Field<ListGraphType<NonNullGraphType<__Type>>>("interfaces").ResolveAsync(async context =>
            {
                if (context.Source is IImplementInterfaces type)
                {
                    var interfaces = context.ArrayPool.Rent<IInterfaceGraphType>(type.ResolvedInterfaces.Count);

                    int index = 0;
                    foreach (var resolved in type.ResolvedInterfaces.List)
                    {
                        if (await context.Schema.Filter.AllowType(resolved).ConfigureAwait(false))
                            interfaces[index++] = resolved;
                    }

                    var comparer = context.Schema.Comparer.TypeComparer;
                    if (comparer != null)
                        Array.Sort(interfaces, 0, index, comparer);

                    return interfaces.Constrained(index);
                }

                return null;
            });

            Field<ListGraphType<NonNullGraphType<__Type>>>("possibleTypes").ResolveAsync(async context =>
            {
                if (context.Source is IAbstractGraphType type)
                {
                    var possibleTypes = context.ArrayPool.Rent<IObjectGraphType>(type.PossibleTypes.Count);

                    int index = 0;
                    foreach (var possible in type.PossibleTypes.List)
                    {
                        if (await context.Schema.Filter.AllowType(possible).ConfigureAwait(false))
                            possibleTypes[index++] = possible;
                    }

                    var comparer = context.Schema.Comparer.TypeComparer;
                    if (comparer != null)
                        Array.Sort(possibleTypes, 0, index, comparer);

                    return possibleTypes.Constrained(index);
                }

                return null;
            });

            Field<ListGraphType<NonNullGraphType<__EnumValue>>>("enumValues")
                .Argument<BooleanGraphType>("includeDeprecated", arg => arg.DefaultValue = BoolBox.False)
                .ResolveAsync(async context =>
                {
                    if (context.Source is EnumerationGraphType type)
                    {
                        var enumValueDefinitions = context.ArrayPool.Rent<EnumValueDefinition>(type.Values.Count);

                        bool includeDeprecated = context.GetArgument<bool>("includeDeprecated");

                        int index = 0;
                        foreach (var def in type.Values) //ISSUE:allocation
                        {
                            if ((includeDeprecated || string.IsNullOrWhiteSpace(def.DeprecationReason)) && await context.Schema.Filter.AllowEnumValue(type, def).ConfigureAwait(false))
                                enumValueDefinitions[index++] = def;
                        }

                        var comparer = context.Schema.Comparer.EnumValueComparer(type);
                        if (comparer != null)
                            Array.Sort(enumValueDefinitions, 0, index, comparer);

                        return enumValueDefinitions.Constrained(index);
                    }

                    return null;
                });

            Field<ListGraphType<NonNullGraphType<__InputValue>>>("inputFields").ResolveAsync(async context =>
            {
                if (context.Source is IInputObjectGraphType type)
                {
                    var inputFields = context.ArrayPool.Rent<FieldType>(type.Fields.Count);

                    int index = 0;
                    foreach (var field in type.Fields.List)
                    {
                        if (await context.Schema.Filter.AllowField(type, field).ConfigureAwait(false))
                            inputFields[index++] = field;
                    }

                    var comparer = context.Schema.Comparer.FieldComparer(type);
                    if (comparer != null)
                        Array.Sort(inputFields, 0, index, comparer);

                    return inputFields.Constrained(index);
                }

                return null;
            });

            Field<__Type>("ofType").Resolve(context =>
            {
                return context.Source switch
                {
                    NonNullGraphType nonNull => nonNull.ResolvedType,
                    ListGraphType list => list.ResolvedType,
                    _ => null
                };
            });

            if (allowAppliedDirectives)
                this.AddAppliedDirectivesField("type");
        }

        private static object KindForInstance(IGraphType type) => type switch
        {
            EnumerationGraphType _ => TypeKindBoxed.ENUM,
            ScalarGraphType _ => TypeKindBoxed.SCALAR,
            IObjectGraphType _ => TypeKindBoxed.OBJECT,
            IInterfaceGraphType _ => TypeKindBoxed.INTERFACE,
            UnionGraphType _ => TypeKindBoxed.UNION,
            IInputObjectGraphType _ => TypeKindBoxed.INPUT_OBJECT,
            ListGraphType _ => TypeKindBoxed.LIST,
            NonNullGraphType _ => TypeKindBoxed.NON_NULL,
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown kind of type: {type}")
        };
    }
}
