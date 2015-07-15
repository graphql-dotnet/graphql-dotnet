using System;

namespace GraphQL.Types
{
    public class EnumerationGraphType : ScalarGraphType
    {
        public override object Coerce(object value)
        {
            throw new NotImplementedException();
        }
    }
}
