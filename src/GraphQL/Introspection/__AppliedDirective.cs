using System;
using System.Linq;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__AppliedDirective</c> introspection type represents a directive applied to a schema element - type, field, argument, etc.
    /// </summary>
    public class __AppliedDirective : ObjectGraphType<AppliedDirective>
    {
        /// <summary>
        /// Initializes a new instance of this graph type.
        /// </summary>
        public __AppliedDirective()
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
                    var applied = context.Source;
                    var directive = context.Schema.Directives.Find(applied.Name);

                    return directive?.Arguments?.Select(arg =>
                    {
                        var appliedArg = applied.Arguments?.FirstOrDefault(a => a.Name == arg.Name);

                        if (appliedArg != null)
                        {
                            if (appliedArg.ResolvedType == null)
                                appliedArg.ResolvedType = arg.ResolvedType;
                            return appliedArg;
                        }
                        else if (arg.DefaultValue != null)
                        {
                            return new DirectiveArgument(arg.Name) //TODO: return QueryArgument instead of DirectiveArgument?
                            {
                                Value = arg.DefaultValue,
                                ResolvedType = arg.ResolvedType
                            };
                        }
                        else
                        {
                            return null;
                        }
                    }) ?? Array.Empty<DirectiveArgument>();
                });
        }
    }
}
