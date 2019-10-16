using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Introspection
{
    public class __DirectiveValue : ObjectGraphType<KeyValuePair<string, SchemaDirectiveVisitor>>
    {
        public __DirectiveValue()
        {
            Name = nameof(__DirectiveValue);
            Description =
                "Schema Directive applied to a element";

            Field<NonNullGraphType<StringGraphType>>(
                "name",
                "Directive name",
                resolve: context => context.Source.Key);
            Field<ListGraphType<NonNullGraphType<__ArgumentValue>>>(
                "args",
                "Values of Directive arguments",
                resolve: context =>
                {
                    var parameter = context.Source;
                    if (parameter.Value?.Arguments == null) return null;

                    // get directive description from schema
                    var directiveType = context.Schema.Directives.FirstOrDefault(d => d.Name == context.Source.Key);

                    return directiveType?.Arguments.Select(a =>
                    {
                        var argValue = parameter.Value.Arguments.FirstOrDefault(p => p.Key == a.Name);

                        return new ParamValue
                        {
                            Name = a.Name,
                            Value = argValue.Value,
                            ResolvedType = a.ResolvedType
                        };
                    });
                });
        }
    }
}
