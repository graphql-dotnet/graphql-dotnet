using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Types;

namespace GraphQL.Introspection.Extended
{
    /// <summary>
    /// Extended introspection for <see cref="__Field"/>. Adds "directives" field.
    /// </summary>
    public class __ExtendedField : __Field
    {
        public __ExtendedField()
        {
            Field<ListGraphType<__DirectiveValue>>(
                name: "directives",
                description: "Directives applied to the field",
                resolve: context => context.Source.GetDirectives());
        }
    }
}
