using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL
{
    public static class GraphQL
    {
        public static ObjectGraphType<T> ObjectType<T>(Action<ObjectGraphType<T>> create)
        {
            var type = new ObjectGraphType<T>();

            create(type);
            return type;
        }
    }

    public class Usage
    {
        public Usage()
        {
            GraphQL.ObjectType<Droid>
        }
    }
}
