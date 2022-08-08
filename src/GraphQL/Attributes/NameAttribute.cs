using GraphQL.Conversion;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Specifies a GraphQL type name for a CLR class, or a field name for a property.
    /// Note that the specified name will be translated by the schema's <see cref="INameConverter"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class NameAttribute : GraphQLAttribute
    {
        private string _name;

        /// <inheritdoc cref="NameAttribute"/>
        public NameAttribute(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Returns the GraphQL name of the associated graph type or field.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc/>
        public override void Modify(EnumValueDefinition enumValueDefinition)
            => enumValueDefinition.Name = Name;

        /// <inheritdoc/>
        public override void Modify(IGraphType graphType)
            => graphType.Name = Name;

        /// <inheritdoc/>
        public override void Modify(FieldType fieldType, bool isInputType)
            => fieldType.Name = Name;

        /// <inheritdoc/>
        public override void Modify(QueryArgument queryArgument)
            => queryArgument.Name = Name;
    }
}
