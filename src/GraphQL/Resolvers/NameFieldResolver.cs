using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Resolvers
{
    internal class NameFieldResolver : IFieldResolver
    {
        private BindingFlags _flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

        public object Resolve(ResolveFieldContext context)
        {
            var source = context.Source;
            if (source == null)
            {
                return null;
            }

            return source
                .GetType()
                .GetProperty(context.FieldAst.Name, _flags)
                .GetValue(source, null);
        }
    }
}
