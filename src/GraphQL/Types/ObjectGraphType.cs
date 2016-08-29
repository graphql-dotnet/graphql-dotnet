using GraphQL.Builders;
using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    public class ObjectGraphType : ComplexGraphType, IImplementInterfaces
    {
        private readonly List<Type> _interfaces = new List<Type>();

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

        public void Interface(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (!type.IsSubclassOf(typeof(InterfaceGraphType)))
            {
                throw new ArgumentException("Interface must implement InterfaceGraphType", nameof(type));
            }
            _interfaces.Add(type);
        }

        public ConnectionBuilder<TGraphType, object> Connection<TGraphType>()
            where TGraphType : ObjectGraphType
        {
            var builder = ConnectionBuilder.Create<TGraphType, object>();
            Field(builder.FieldType);
            return builder;
        }
    }

    public class ObjectGraphType<TSourceType> : ObjectGraphType
    {
        public FieldType Field(
            Type type,
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, object> resolve = null,
            string deprecationReason = null)
        {
            return Field(
                type,
                name,
                description,
                arguments,
                resolve: context => resolve(new ResolveFieldContext<TSourceType>(context)),
                deprecationReason: deprecationReason
            );
        }

        public FieldType Field<TType>(
            string name,
            string description = null,
            QueryArguments arguments = null,
            Func<ResolveFieldContext<TSourceType>, object> resolve = null,
            string deprecationReason = null)
            where TType : GraphType
        {
            return Field(typeof(TType), name, description, arguments, resolve, deprecationReason);
        }

        public new FieldBuilder<TGraphType, TSourceType, object> Field<TGraphType>()
            where TGraphType : GraphType
        {
            var builder = FieldBuilder.Create<TGraphType, TSourceType>();
            Field(builder.FieldType);
            return builder;
        }

        public new ConnectionBuilder<TGraphType, TSourceType> Connection<TGraphType>()
            where TGraphType : ObjectGraphType
        {
            var builder = ConnectionBuilder.Create<TGraphType, TSourceType>();
            Field(builder.FieldType);
            return builder;
        }
    }
}
