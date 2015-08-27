using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public class Schema
    {
        public Schema()
        {
            ResolveType = type => (GraphType)Activator.CreateInstance(type);
        }

        private GraphTypesLookup _lookup;

        public ObjectGraphType Query { get; set; }

        public ObjectGraphType Mutation { get; set; }

        public Func<Type, GraphType> ResolveType { get; set; }

        public IEnumerable<DirectiveGraphType> Directives
        {
            get
            {
                return new List<DirectiveGraphType>
                {
                    DirectiveGraphType.Include,
                    DirectiveGraphType.Skip
                };
            }
        }

        public IEnumerable<GraphType> AllTypes
        {
            get
            {
                return _lookup
                    .All()
                    .Where(x => !(x is NonNullGraphType || x is ListGraphType))
                    .ToList();
            }
        }

        public GraphType FindType(string name)
        {
            EnsureLookup();
            return _lookup[name];
        }

        public GraphType FindType(Type type)
        {
            EnsureLookup();
            return _lookup[type] ?? AddType(type);
        }

        public IEnumerable<GraphType> FindTypes(IEnumerable<Type> types)
        {
            return types.Select(FindType).ToList();
        }

        public IEnumerable<GraphType> FindImplemenationsOf(Type type)
        {
            return _lookup.FindImplemenationsOf(type);
        }

        private GraphType AddType(Type type)
        {
            var ctx = new TypeCollectionContext(ResolveType, (name, graphType) =>
            {
                _lookup[name] = graphType;
            });

            var instance = ResolveType(type);
            _lookup.AddType(instance, ctx);
            return instance;
        }

        public void EnsureLookup()
        {
            if (_lookup == null)
            {
                _lookup = new GraphTypesLookup();

                var ctx = new TypeCollectionContext(ResolveType, (name, graphType) =>
                {
                    _lookup[name] = graphType;
                });

                _lookup.AddType(Query, ctx);
                _lookup.AddType(Mutation, ctx);
            }
        }
    }
}
