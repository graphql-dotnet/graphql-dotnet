using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Introspection
{
    public class __DirectiveUsage : ObjectGraphType<KeyValuePair<string, SchemaDirectiveVisitor>>
    {
        public __DirectiveUsage()
        {
            Description =
                "Directive applied to a schema element";

            Field<NonNullGraphType<StringGraphType>>(
                "name",
                "Directive name",
                resolve: context => context.Source.Key);
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__DirectiveArgument>>>>(
                "args",
                "Values of Directive arguments",
                resolve: context =>
                {
                    var parameter = context.Source;
                    if (parameter.Value?.Arguments == null) return Array.Empty<ParamValue>();

                    // get directive description from schema
                    var directiveType = context.Schema.Directives.FirstOrDefault(d => d.Name == parameter.Key);

                    return directiveType?.Arguments.Select(arg =>
                    {
                        var argValue = parameter.Value.Arguments.FirstOrDefault(p => p.Key == arg.Name);

                        return new ParamValue
                        {
                            Name = arg.Name,
                            Value = argValue.Value ?? arg.DefaultValue,
                            ResolvedType = arg.ResolvedType
                        };
                    });
                });
        }
    }
}
