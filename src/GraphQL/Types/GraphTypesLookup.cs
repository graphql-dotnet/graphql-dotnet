using System.Collections.Generic;

namespace GraphQL.Types
{
    public class GraphTypesLookup
    {
        private readonly Dictionary<string, GraphType> _types = new Dictionary<string, GraphType>();

        public GraphTypesLookup()
        {
            _types[ScalarGraphType.String.ToString()] = ScalarGraphType.String;
            _types[ScalarGraphType.Boolean.ToString()] = ScalarGraphType.Boolean;
            _types[ScalarGraphType.Float.ToString()] = ScalarGraphType.Float;
            _types[ScalarGraphType.Int.ToString()] = ScalarGraphType.Int;
            _types[ScalarGraphType.Id.ToString()] = ScalarGraphType.Id;

            _types[NonNullGraphType.String.ToString()] = NonNullGraphType.String;
            _types[NonNullGraphType.Boolean.ToString()] = NonNullGraphType.Boolean;
            _types[NonNullGraphType.Float.ToString()] = NonNullGraphType.Float;
            _types[NonNullGraphType.Int.ToString()] = NonNullGraphType.Int;
            _types[NonNullGraphType.Id.ToString()] = NonNullGraphType.Id;
        }

        public GraphType this[string typeName]
        {
            get
            {
                GraphType result = null;
                if (_types.ContainsKey(typeName))
                {
                    result = _types[typeName];
                }

                return result;
            }
            set { _types[typeName] = value; }
        }
    }
}
