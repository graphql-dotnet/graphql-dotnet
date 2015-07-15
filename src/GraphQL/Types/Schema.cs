namespace GraphQL.Types
{
    public class Schema
    {
        private GraphTypesLookup _lookup;

        public ObjectGraphType Query { get; set; }

        public ObjectGraphType Mutation { get; set; }

        public GraphType FindType(string name)
        {
            if (_lookup == null)
            {
                _lookup = new GraphTypesLookup();
                CollectTypes(Query, _lookup);
                CollectTypes(Mutation, _lookup);
            }

            return _lookup[name];
        }

        private void CollectTypes(GraphType type, GraphTypesLookup lookup)
        {
            if (type == null)
            {
                return;
            }

            lookup[type.ToString()] = type;

            type.Fields.Apply(field =>
            {
                CollectTypes(field.Type, lookup);
            });
        }
    }
}
