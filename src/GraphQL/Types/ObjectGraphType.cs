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
                IsTypeOf = instance => instance is TSourceType;
        }

        public void AddResolvedInterface(IInterfaceGraphType graphType)
        {
            if (!_resolvedInterfaces.Contains(graphType))
            {
                graphType.IsValidInterfaceFor(this, throwError: true);
                _resolvedInterfaces.Add(graphType ?? throw new ArgumentNullException(nameof(graphType)));
            }
        }

        public IEnumerable<IInterfaceGraphType> ResolvedInterfaces
        {
            get { return _resolvedInterfaces; }
            set
            {
                _resolvedInterfaces.Clear();

                if (value != null)
                {
                    foreach (var item in value)
                        _resolvedInterfaces.Add(item ?? throw new ArgumentNullException(nameof(value), "value contains null item"));
                }
            }
        }

        public IEnumerable<Type> Interfaces
        {
            get { return _interfaces; }
            set
            {
                _interfaces.Clear();

                if (value != null)
                {
                    foreach (var item in value)
                        _interfaces.Add(item ?? throw new ArgumentNullException(nameof(value), "value contains null item"));
                }
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
                throw new ArgumentException($"Interface must implement {nameof(IInterfaceGraphType)}", nameof(type));
            }

            if (!_interfaces.Contains(type))
                _interfaces.Add(type);
        }
    }

    public class ObjectGraphType : ObjectGraphType<object>
    {
    }
}
