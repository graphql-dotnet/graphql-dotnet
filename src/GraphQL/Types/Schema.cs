using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public interface ISchema
    {
        ObjectGraphType Query { get; set; }

        ObjectGraphType Mutation { get; set; }

        IEnumerable<DirectiveGraphType> Directives { get; }

        IEnumerable<GraphType> AllTypes { get; }

        GraphType FindType(string name);

        GraphType FindType(Type type);

        IEnumerable<GraphType> FindTypes(IEnumerable<Type> types);

        IEnumerable<GraphType> FindImplementationsOf(Type type);
    }

    public class Schema : ISchema
    {
        public Schema()
            : this(type => (GraphType) Activator.CreateInstance(type))
        {
        }

        public Schema(Func<Type, GraphType> resolveType)
        {
            ResolveType = resolveType;
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

        public IEnumerable<GraphType> FindImplementationsOf(Type type)
        {
            return _lookup.FindImplemenationsOf(type);
        }

        private GraphType AddType(Type type)
        {
            var ctx = new TypeCollectionContext(ResolveType, (name, graphType, context) =>
            {
                _lookup.AddType(graphType, context);
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

                var ctx = new TypeCollectionContext(ResolveType, (name, graphType, context) =>
                {
                    if (_lookup[name] == null)
                    {
                        _lookup.AddType(graphType, context);
                    }
                });

                _lookup.AddType(Query, ctx);
                _lookup.AddType(Mutation, ctx);
            }
        }
    }
}
