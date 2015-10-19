using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL.Types
{
    public abstract class GraphType
    {
        private readonly List<FieldType> _fields = new List<FieldType>();

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<FieldType> Fields
        {
            get { return _fields; }
            private set
            {
                _fields.Clear();
                _fields.AddRange(value);
            }
        }

        public void Field<TType>(
            string name, 
            string description = null, 
            QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
            where TType : GraphType
        {
            if (_fields.Exists(x => x.Name == name))
            {
                throw new ArgumentOutOfRangeException("name", "A field with that name is already registered.");
            }

            _fields.Add(new FieldType
            {
                Name = name,
                Type = typeof(TType),
                Arguments = arguments,
                Resolve = resolve
            });
        }

        public virtual string CollectTypes(TypeCollectionContext context)
        {
            return Name;
        }
    }

    /// <summary>
    /// This sucks, find a better way
    /// </summary>
    public class TypeCollectionContext
    {
        public TypeCollectionContext(
            Func<Type, GraphType> resolver,
            Action<string, GraphType> addType)
        {
            ResolveType = resolver;
            AddType = addType;
        }

        public TypeCollectionContext(
            Func<Type, GraphType> resolver,
            Action<string, GraphType, TypeCollectionContext> addType)
        {
            ResolveType = resolver;
            AddType = ( name, type ) => addType( name, type, this );
        }

        public Func<Type, GraphType> ResolveType { get; private set; }
        public Action<string, GraphType> AddType { get; private set; }
    }
}
