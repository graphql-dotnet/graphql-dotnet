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
            if (graphType == null)
                throw new ArgumentNullException(nameof(graphType));

            _ = graphType.IsValidInterfaceFor(this, throwError: true);
            ResolvedInterfaces.Add(graphType);
        }

        /// <inheritdoc/>
        public ResolvedInterfaces ResolvedInterfaces { get; } = new ResolvedInterfaces();

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
        /// Adds a GraphQL interface graph type to the list of GraphQL interfaces implemented by this graph type.
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
                throw new ArgumentException($"Interface '{type.Name}' must implement {nameof(IInterfaceGraphType)}", nameof(type));
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
