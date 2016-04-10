using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public interface ISchema : IDisposable
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
        private readonly Lazy<GraphTypesLookup> _lookup;

        public Schema()
            : this(type => (GraphType) Activator.CreateInstance(type))
        {
        }

        public Schema(Func<Type, GraphType> resolveType)
        {
            ResolveType = resolveType;

            _lookup = new Lazy<GraphTypesLookup>(CreateTypesLookup);
        }

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
                    .Value
                    .All()
                    .Where(x => !(x is NonNullGraphType || x is ListGraphType))
                    .ToList();
            }
        }

        public GraphType FindType(string name)
        {
            return _lookup.Value[name];
        }

        public GraphType FindType(Type type)
        {
            return _lookup.Value[type] ?? AddType(type);
        }

        public IEnumerable<GraphType> FindTypes(IEnumerable<Type> types)
        {
            return types.Select(FindType).ToList();
        }

        public IEnumerable<GraphType> FindImplementationsOf(Type type)
        {
            return _lookup.Value.FindImplemenationsOf(type);
        }

        public void Dispose()
        {
            ResolveType = null;
            Query = null;
            Mutation = null;

            _lookup.Value.Clear();
        }

        private GraphTypesLookup CreateTypesLookup()
        {
            return GraphTypesLookup.Create(new[] { Query, Mutation }, ResolveType);
        }

        private GraphType AddType(Type type)
        {
            var ctx = new TypeCollectionContext(ResolveType, (name, graphType, context) =>
            {
                _lookup.Value.AddType(graphType, context);
            });

            var instance = ResolveType(type);
            _lookup.Value.AddType(instance, ctx);
            return instance;
        }
    }
}
