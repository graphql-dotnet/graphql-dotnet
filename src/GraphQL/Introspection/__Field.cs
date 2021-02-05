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
                var description = context.Source.Description;

                // https://github.com/graphql-dotnet/graphql-dotnet/issues/1004
                if (description == null)
                {
                    // We have to iterate over all schema types because FieldType has no reference to the GraphType to which it belongs.
                    var fieldOwner = context.Schema.AllTypes.OfType<IComplexGraphType>().Single(t => t.Fields.Contains(context.Source));
                    if (fieldOwner is IImplementInterfaces {ResolvedInterfaces: { }} implementation)
                    {
                        foreach (var iface in implementation.ResolvedInterfaces)
                        {
                            var fieldFromInterface = iface.GetField(context.Source.Name);
                            if (fieldFromInterface?.Description != null)
                                return fieldFromInterface.Description;
                        }
                    }
                }

                return description;
            });

            FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<__InputValue>>>>("args",
                resolve: async context =>
                {
                    var arguments = context.Source.Arguments ?? Enumerable.Empty<QueryArgument>();
                    return await arguments.WhereAsync(x => context.Schema.Filter.AllowArgument(context.Source, x)).ConfigureAwait(false);
                });
            Field<NonNullGraphType<__Type>>("type", resolve: ctx => ctx.Source.ResolvedType);
            Field<NonNullGraphType<BooleanGraphType>>("isDeprecated", resolve: context => (!string.IsNullOrWhiteSpace(context.Source.DeprecationReason)).Boxed());
            Field(f => f.DeprecationReason, nullable: true).Description(null);
        }
    }
}
