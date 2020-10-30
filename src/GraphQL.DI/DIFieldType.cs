using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.DI
{
    public class DIFieldType : FieldType
    {
        public bool Concurrent { get; set; } = false;
    }
}
