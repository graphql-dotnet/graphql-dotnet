using System.Collections.Generic;
using GraphQL.Language;
using System.Threading;

namespace GraphQL.Types
{
    public class ResolveFieldContext
    {
        public Field FieldAst { get; set; }

        public FieldType FieldDefinition { get; set; }

        public ObjectGraphType ParentType { get; set; }

        public Dictionary<string, object> Arguments { get; set; }

        public object Source { get; set; }

        public Schema Schema { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public TType Argument<TType>(string name)
        {
            if (Arguments.ContainsKey(name))
            {
                return (TType) Arguments[name];
            }

            return default(TType);
        }
    }
}
