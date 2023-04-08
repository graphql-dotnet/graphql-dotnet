using System.Collections.Concurrent;
using System.Reflection;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL
{
    /// <summary>
    /// Allows additional configuration to be applied to a type, field or query argument definition.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
    public abstract class GraphQLAttribute : Attribute
    {
        /// <summary>
        /// Updates the properties of the specified <see cref="TypeConfig"/> as necessary.
        /// </summary>
        public virtual void Modify(TypeConfig type)
        {
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="FieldConfig"/> as necessary.
        /// </summary>
        public virtual void Modify(FieldConfig field)
        {
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="IGraphType"/> as necessary.
        /// </summary>
        public virtual void Modify(IGraphType graphType)
        {
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="EnumValueDefinition"/> as necessary.
        /// </summary>
        public virtual void Modify(EnumValueDefinition enumValueDefinition)
        {
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="FieldType"/> as necessary.
        /// </summary>
        /// <param name="fieldType">The <see cref="FieldType"/> to update.</param>
        /// <param name="isInputType">Indicates if the graph type containing this field is an input type.</param>
        public virtual void Modify(FieldType fieldType, bool isInputType)
        {
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="TypeInformation"/> as necessary.
        /// </summary>
        public virtual void Modify(TypeInformation typeInformation)
        {
        }

        private static readonly MethodInfo _modifyMethod = typeof(GraphQLAttribute)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.Name == nameof(GraphQLAttribute.Modify) && x.IsGenericMethod)
            .Single();
        private static readonly ConcurrentDictionary<Type, MethodInfo> _typedModifyMethods = new();

        /// <summary>
        /// Updates the properties of the specified <see cref="ArgumentInformation"/> as necessary.
        /// </summary>
        public virtual void Modify(ArgumentInformation argumentInformation)
        {
            var typedMethod = _typedModifyMethods.GetOrAdd(
                argumentInformation.ParameterInfo.ParameterType,
                type => _modifyMethod.MakeGenericMethod(type));
            var func = (Action<ArgumentInformation>)typedMethod.CreateDelegate(typeof(Action<ArgumentInformation>), this);
            func(argumentInformation);
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="ArgumentInformation"/> as necessary.
        /// <typeparamref name="TParameterType"/> represents the return type of the parameter.
        /// </summary>
        public virtual void Modify<TParameterType>(ArgumentInformation argumentInformation)
        {
        }

        /// <summary>
        /// Updates the properties of the specified <see cref="QueryArgument"/> as necessary.
        /// </summary>
        public virtual void Modify(QueryArgument queryArgument)
        {
        }

        /// <summary>
        /// Determines if a specified member should be included during automatic generation
        /// of a graph type from a CLR type.
        /// <br/><br/>
        /// When called for enumeration values, <paramref name="isInputType"/> is <see langword="null"/>.
        /// </summary>
        public virtual bool ShouldInclude(MemberInfo memberInfo, bool? isInputType) => true;

        /// <summary>
        /// Determines the order in which GraphQL attributes are applied to the graph type, field type, or parameter definition.
        /// Attributes with the lowest <see cref="Priority"/> value are applied first.
        /// The default priority is 1.
        /// </summary>
        public virtual float Priority => 1.0f;
    }
}
