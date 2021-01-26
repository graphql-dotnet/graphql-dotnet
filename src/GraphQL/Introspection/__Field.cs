using System;
using System.Linq;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__Field</c> introspection type represents each field in an Object or Interface type.
    /// </summary>
    public class __Field : ObjectGraphType<IFieldType>
    {
        /// <summary>
        /// Initializes a new instance of the <c>__Field</c> introspection type.
        /// </summary>
        public __Field()
        {
            Name = nameof(__Field);

            Description =
                "Object and Interface types are described by a list of Fields, each of " +
                "which has a name, potentially a list of arguments, and a return type.";

            Field(f => f.Name).Description(null);

            Field<StringGraphType>("description", resolve: context =>
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

            FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<__InputValue>>>>("args",
                resolve: async context =>
                {
                    var source = context.Source;
                    if (source.Arguments?.Count > 0)
                    {
                        var arguments = context.ArrayPool.Rent<QueryArgument>(source.Arguments.Count);

                        int index = 0;
                        foreach (var argument in source.Arguments.List)
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

            Field<NonNullGraphType<__Type>>("type", resolve: ctx => ctx.Source.ResolvedType);

            Field<NonNullGraphType<BooleanGraphType>>("isDeprecated", resolve: context => (!string.IsNullOrWhiteSpace(context.Source.DeprecationReason)).Boxed());

            Field(f => f.DeprecationReason, nullable: true).Description(null);

            FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<__AppliedDirective>>>>(
                name: "directives",
                description: "Directives applied to the field",
                resolve: async context =>
                {
                    if (context.Source.AppliedDirectives?.Count > 0)
                    {
                        var usages = context.ArrayPool.Rent<AppliedDirective>(context.Source.AppliedDirectives.Count);

                        int index = 0;
                        foreach (var usage in usages)
                        {
                            // return only registered directives allowed by filter
                            var schemaDirective = context.Schema.Directives.FirstOrDefault(directive => directive.Name == usage.Name);
                            if (schemaDirective != null && await context.Schema.Filter.AllowDirective(schemaDirective))
                            {
                                usages[index++] = usage;
                            }
                        }

                        return usages.Constrained(index);
                    }

                    return Array.Empty<AppliedDirective>();
                });
        }
    }
}
