using GraphQL.Conversion;
using GraphQL.Types;

namespace GraphQL
{
    /// <summary>
    /// Specifies a GraphQL type name for a CLR class when used as an output type.
    /// Note that the specified name will be translated by the schema's <see cref="INameConverter"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
    public class OutputNameAttribute : GraphQLAttribute
    {
        private string _name;

        /// <inheritdoc cref="NameAttribute"/>
        public OutputNameAttribute(string name)
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
        public override void Modify(IGraphType graphType)
        {
            if (graphType is not IInputObjectGraphType)
            {
                graphType.Name = Name;
            }
        }

        /// <inheritdoc/>
        public override void Modify(FieldType fieldType, bool isInputType)
        {
            if (!isInputType)
                fieldType.Name = Name;
        }
    }
}
