using System;
using GraphQL.Types;
using System.Linq;

namespace GraphQL.Introspection
{
    public class __Directive : ObjectGraphType
    {
        public __Directive()
        {
            Name = "__Directive";
            Description =
                "A Directive provides a way to describe alternate runtime execution and " +
                "type validation behavior in a GraphQL document." +
                $"{Environment.NewLine}{Environment.NewLine}In some cases, you need to provide options to alter GraphQL\"s " +
                "execution behavior in ways field arguments will not suffice, such as " +
                "conditionally including or skipping a field. Directives provide this by " +
                "describing additional information to the executor.";
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__InputValue>>>>("args", resolve: context =>
            {
                var fieldType = (DirectiveGraphType) context.Source;
                return fieldType.Arguments ?? Enumerable.Empty<QueryArgument>();
            });
            Field<NonNullGraphType<BooleanGraphType>>("onOperation");
            Field<NonNullGraphType<BooleanGraphType>>("onFragment");
            Field<NonNullGraphType<BooleanGraphType>>("onField");
        }
    }
}
