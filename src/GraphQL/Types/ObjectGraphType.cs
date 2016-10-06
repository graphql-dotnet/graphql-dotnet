using GraphQL.Builders;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace GraphQL.Types
{
    public interface IObjectGraphType : IComplexGraphType, IImplementInterfaces
    {
        Func<object, bool> IsTypeOf { get; set; }
        void AddResolvedInterface(IInterfaceGraphType graphType);
    }

    public class ObjectGraphType : ComplexGraphType<object>, IObjectGraphType
    {
        private readonly List<Type> _interfaces = new List<Type>();
        private readonly List<IInterfaceGraphType> _resolvedInterfaces = new List<IInterfaceGraphType>();

        public Func<object, bool> IsTypeOf { get; set; }

        public void AddResolvedInterface(IInterfaceGraphType graphType)
        {
            _resolvedInterfaces.Add(graphType);
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
            _interfaces.Add(typeof(TInterface));
        }

        public void Interface(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (!type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IInterfaceGraphType)))
            {
                throw new ArgumentException("Interface must implement IInterfaceGraphType", nameof(type));
            }
            _interfaces.Add(type);
        }

        public ConnectionBuilder<TNodeType, object> Connection<TNodeType>()
            where TNodeType : IGraphType
        {
            var builder = ConnectionBuilder.Create<TNodeType, object>();
            AddField(builder.FieldType);
            return builder;
        }
    }

    public class ObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IObjectGraphType
    {
        private readonly List<Type> _interfaces = new List<Type>();
        private readonly List<IInterfaceGraphType> _resolvedInterfaces = new List<IInterfaceGraphType>();

        public Func<object, bool> IsTypeOf { get; set; }
            = type => type is TSourceType;

        public void AddResolvedInterface(IInterfaceGraphType graphType)
        {
            _resolvedInterfaces.Add(graphType);
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
            _interfaces.Add(typeof(TInterface));
        }

        public void Interface(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (!type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IInterfaceGraphType)))
            {
                throw new ArgumentException("Interface must implement IInterfaceGraphType", nameof(type));
            }
            _interfaces.Add(type);
        }

        public ConnectionBuilder<TNodeType, TSourceType> Connection<TNodeType>()
            where TNodeType : IGraphType
        {
            var builder = ConnectionBuilder.Create<TNodeType, TSourceType>();
            AddField(builder.FieldType);
            return builder;
        }
    }
}
