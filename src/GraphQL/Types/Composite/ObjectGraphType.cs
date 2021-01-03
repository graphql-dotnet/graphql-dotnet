using System;
using System.Collections.Generic;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents an interface for all object (that is, having their own properties) output graph types.
    /// </summary>
    public interface IObjectGraphType : IComplexGraphType, IImplementInterfaces
    {
        Func<object, bool> IsTypeOf { get; set; }
        void AddResolvedInterface(IInterfaceGraphType graphType);
    }

    /// <summary>
    /// Represents a default base class for all object (that is, having their own properties) output graph types.
    /// </summary>
    /// <typeparam name="TSourceType">Typically the type of the object that this graph represents. More specifically, the .NET type of the source property within field resolvers for this graph.</typeparam>
    public class ObjectGraphType<TSourceType> : ComplexGraphType<TSourceType>, IObjectGraphType
    {
        private readonly List<Type> _interfaces = new List<Type>();
        private readonly List<IInterfaceGraphType> _resolvedInterfaces = new List<IInterfaceGraphType>();

        /// <inheritdoc/>
        public Func<object, bool> IsTypeOf { get; set; }

        /// <inheritdoc/>
        public ObjectGraphType()
        {
            if (typeof(TSourceType) != typeof(object))
                IsTypeOf = instance => instance is TSourceType;
        }

        /// <inheritdoc/>
        public void AddResolvedInterface(IInterfaceGraphType graphType)
        {
            if (!_resolvedInterfaces.Contains(graphType))
            {
                graphType.IsValidInterfaceFor(this, throwError: true);
                _resolvedInterfaces.Add(graphType ?? throw new ArgumentNullException(nameof(graphType)));
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IInterfaceGraphType> ResolvedInterfaces
        {
            get => _resolvedInterfaces;
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

        /// <inheritdoc/>
        public IEnumerable<Type> Interfaces
        {
            get => _interfaces;
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

        /// <summary>
        /// Adds a GraphQL interface graph type to the list of compatible GraphQL interfaces for this graph type.
        /// </summary>
        public void Interface<TInterface>()
            where TInterface : IInterfaceGraphType
        {
            if (!_interfaces.Contains(typeof(TInterface)))
                _interfaces.Add(typeof(TInterface));
        }

        /// <inheritdoc cref="Interface{TInterface}"/>
        public void Interface(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!typeof(IInterfaceGraphType).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Interface must implement {nameof(IInterfaceGraphType)}", nameof(type));
            }

            if (!_interfaces.Contains(type))
                _interfaces.Add(type);
        }
    }

    /// <summary>
    /// Represents a default base class for all object (that is, having their own properties) output graph types.
    /// </summary>
    public class ObjectGraphType : ObjectGraphType<object>
    {
    }
}
