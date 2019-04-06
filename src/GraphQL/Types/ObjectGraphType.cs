using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public interface IObjectGraphType : IComplexGraphType, IImplementInterfaces
    {
        Func<object, bool> IsTypeOf { get; set; }
        void AddResolvedInterface(IInterfaceGraphType graphType);
    }

    public class ObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IObjectGraphType
    {
        private readonly List<Type> _interfaces = new List<Type>();
        private readonly List<IInterfaceGraphType> _resolvedInterfaces = new List<IInterfaceGraphType>();

        public Func<object, bool> IsTypeOf { get; set; }

        public ObjectGraphType()
        {
            if (typeof(TSourceType) != typeof(object))
                IsTypeOf = type => type is TSourceType;
        }

        public void AddResolvedInterface(IInterfaceGraphType graphType)
        {
            if (!_resolvedInterfaces.Contains(graphType))
            {
                _resolvedInterfaces.Add(graphType ?? throw new ArgumentNullException(nameof(graphType)));
            }
        }

        public IEnumerable<IInterfaceGraphType> ResolvedInterfaces
        {
            get { return _resolvedInterfaces; }
            set
            {
                _resolvedInterfaces.Clear();
                _resolvedInterfaces.AddRange(value);
            }
        }

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
            where TInterface : IInterfaceGraphType
        {
            if (!_interfaces.Contains(typeof(TInterface)))
                _interfaces.Add(typeof(TInterface));
        }

        public void Interface(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (!type.GetInterfaces().Contains(typeof(IInterfaceGraphType)))
            {
                throw new ArgumentException("Interface must implement IInterfaceGraphType", nameof(type));
            }
            if (!_interfaces.Contains(type))
                _interfaces.Add(type);
        }
    }

    public class ObjectGraphType : ObjectGraphType<object>
    {
    }
}
