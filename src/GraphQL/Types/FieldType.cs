using System;

namespace GraphQL.Types
{
    public class FieldType : IHaveDefaultValue
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string DeprecationReason { get; set; }

        public object DefaultValue { get; set; }

        public GraphType Type { get; set; }

        public QueryArguments Arguments { get; set; }

        public Func<ResolveFieldContext, object> Resolve { get; set; }
    }
}
