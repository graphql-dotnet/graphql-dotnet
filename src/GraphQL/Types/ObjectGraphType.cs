using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public class ObjectGraphType : GraphType, IImplementInterfaces
    {
        private readonly List<Type> _interfaces;

        public ObjectGraphType()
        {
            _interfaces = new List<Type>();
        }

        public Func<object, bool> IsTypeOf { get; set; }

        public IEnumerable<Type> Interfaces
        {
            get { return _interfaces; }
            set
            {
                _interfaces.Clear();
                _interfaces.AddRange(value);
            }
        }

        public void Interface<TInterface>()
            where TInterface : InterfaceGraphType
        {
            _interfaces.Add(typeof(TInterface));
        }
    }
}
