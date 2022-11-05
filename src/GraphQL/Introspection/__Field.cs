using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <see cref="__Field"/> introspection type represents each field in an Object or Interface type.
    /// </summary>
    public class __Field : ObjectGraphType<IFieldType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="__Field"/> introspection type.
        /// </summary>
        /// <param name="allowAppliedDirectives">Allows 'appliedDirectives' field for this type. It is an experimental feature.</param>
        public __Field(bool allowAppliedDirectives = false)
        {
            Name = nameof(__Field);

            Description =
                "Object and Interface types are described by a list of Fields, each of " +
                "which has a name, potentially a list of arguments, and a return type.";

            Field<NonNullGraphType<StringGraphType>>("name").Resolve(context => context.Source.Name);

            Field<StringGraphType>("description").Resolve(context =>
            {
                string description = context.Source.Description;

                // https://github.com/graphql-dotnet/graphql-dotnet/issues/1004
                if (description == null)
                {
                    // We have to iterate over all schema types because FieldType has no reference to the GraphType to which it belongs.
                    foreach (var item in context.Schema.AllTypes.Dictionary)
                    {
                        if (item.Value is IComplexGraphType fieldOwner && fieldOwner.Fields.Contains(context.Source))
                        {
                            if (fieldOwner is IImplementInterfaces implementation && implementation.ResolvedInterfaces != null)
                            {
                                foreach (var iface in implementation.ResolvedInterfaces.List)
                                {
                                    var fieldFromInterface = iface.GetField(context.Source.Name);
                                    if (fieldFromInterface?.Description != null)
                                        return fieldFromInterface.Description;
                                }
                            }
                        }
                    }
                }

                return description;
            });

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__InputValue>>>>("args")
                .ResolveAsync(async context =>
                {
                    var source = context.Source;
                    if (source.Arguments?.Count > 0)
                    {
                        var arguments = context.ArrayPool.Rent<QueryArgument>(source.Arguments.Count);

                        int index = 0;
                        foreach (var argument in source.Arguments.List!)
                        {
                            if (await context.Schema.Filter.AllowArgument(source, argument).ConfigureAwait(false))
                                arguments[index++] = argument;
                        }

                        var comparer = context.Schema.Comparer.ArgumentComparer(source);
                        if (comparer != null)
                            Array.Sort(arguments, 0, index, comparer);

                        return arguments.Constrained(index);
                    }

                    return Array.Empty<QueryArgument>();
                });

            Field<NonNullGraphType<__Type>>("type").Resolve(context => context.Source.ResolvedType);

            Field<NonNullGraphType<BooleanGraphType>>("isDeprecated").Resolve(context => (!string.IsNullOrWhiteSpace(context.Source.DeprecationReason)).Boxed());

            Field<StringGraphType>("deprecationReason").Resolve(context => context.Source.DeprecationReason);

            if (allowAppliedDirectives)
                this.AddAppliedDirectivesField("field");
        }
    }
}
