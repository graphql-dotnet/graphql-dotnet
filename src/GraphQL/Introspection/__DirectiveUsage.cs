using GraphQL.Types;
using GraphQL.Utilities;
using System;
using System.Linq;

namespace GraphQL.Introspection
{
    public class __DirectiveUsage : ObjectGraphType<SchemaDirectiveVisitor>
    {
        public __DirectiveUsage()
        {
            Description =
                "Directive applied to a schema element";

            Field<NonNullGraphType<StringGraphType>>(
                "name",
                "Directive name",
                resolve: context => context.Source.Name);

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__DirectiveArgument>>>>(
                "args",
                "Values of directive arguments",
                resolve: context =>
                {
                    var visitor = context.Source;
                    if (visitor.Arguments == null) return Array.Empty<DirectiveArgumentValue>();

                    // get registered directive from schema
                    var registeredDirective = context.Schema.Directives.FirstOrDefault(directive => directive.Name == visitor.Name);

                    return registeredDirective?.Arguments.Select(arg =>
                    {
                        return new DirectiveArgumentValue
                        {
                            Name = arg.Name,
                            Value = visitor.Arguments.TryGetValue(arg.Name, out var value) ? value : arg.DefaultValue,
                            ResolvedType = arg.ResolvedType
                        };
                    });
                });
        }
    }
}
