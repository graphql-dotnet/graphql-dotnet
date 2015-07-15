using System;
using System.Collections.Generic;

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

        public void Field(string name, string description, GraphType type, QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
        {
            if (_fields.Exists(x => x.Name == name))
            {
                throw new ArgumentOutOfRangeException("name", "A field with that name is already registered.");
            }

            _fields.Add(new FieldType
            {
                Name = name,
                Type = type,
                Arguments = arguments,
                Resolve = resolve
            });
        }

        public void Field(string name, GraphType type, QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
        {
            Field(name, null, type, arguments, resolve);
        }

        public void Field<TType>(string name, string description = null, QueryArguments arguments = null,
            Func<ResolveFieldContext, object> resolve = null)
            where TType : GraphType, new()
        {
            Field(name, description, new TType(), arguments, resolve);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
