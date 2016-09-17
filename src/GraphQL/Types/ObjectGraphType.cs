using GraphQL.Builders;
using System;
using System.Collections.Generic;

namespace GraphQL.Types
{   
    public interface IObjectGraphType : IComplexGraphType, IImplementInterfaces
    {
        Func<object, bool> IsTypeOf { get; set; }
    }

    public class ObjectGraphType : ComplexGraphType<object>, IObjectGraphType
    {
        private readonly List<IInterfaceGraphType> _interfaces = new List<IInterfaceGraphType>();

        public Func<object, bool> IsTypeOf { get; set; }

        public IEnumerable<IInterfaceGraphType> Interfaces
        {
            get { return _interfaces; }
            set
            {
                _interfaces.Clear();
                _interfaces.AddRange(value);
            }
        }

        public ObjectGraphType()
        {
        }

        public ObjectGraphType(Action<ObjectGraphType> configure)
        {
            configure(this);
        }

        public void Interface(IInterfaceGraphType type)
        {
            _interfaces.Add(type);
        }

        public ConnectionBuilder<TNodeType, object> Connection<TNodeType>()
            where TNodeType : IObjectGraphType
        {
            var builder = ConnectionBuilder.Create<TNodeType, object>();
            AddField(builder.FieldType);
            return builder;
        }
    }

    public class ObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IObjectGraphType
    {
        private readonly List<IInterfaceGraphType> _interfaces = new List<IInterfaceGraphType>();

        public Func<object, bool> IsTypeOf { get; set; } 
            = type => type is TSourceType;

        public IEnumerable<IInterfaceGraphType> Interfaces
        {
            get { return _interfaces; }
            set
            {
                _interfaces.Clear();
                _interfaces.AddRange(value);
            }
        }

        public ObjectGraphType()
        {
        }

        public ObjectGraphType(Action<ObjectGraphType<TSourceType>> configure)
        {
            configure(this);
        }


        public void Interface(IInterfaceGraphType type)
        {
            _interfaces.Add(type);
        }

        public ConnectionBuilder<TNodeType, TSourceType> Connection<TNodeType>()
            where TNodeType : IObjectGraphType
        {
            var builder = ConnectionBuilder.Create<TNodeType, TSourceType>();
            AddField(builder.FieldType);
            return builder;
        }
    }
}
