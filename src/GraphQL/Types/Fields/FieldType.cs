using System;
using System.Diagnostics;
using GraphQL.Language.AST;
using GraphQL.Resolvers;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// Represents a field of a graph type.
    /// </summary>
    [DebuggerDisplay("{Name,nq}: {ResolvedType,nq}")]
    public class FieldType : MetadataProvider, IFieldType
    {
        private object _defaultValue;
        private IValue _defaultValueAST;

        private string _name;
        /// <inheritdoc/>
        public string Name
        {
            get => _name;
            set => SetName(value, validate: true);
        }

        internal void SetName(string name, bool validate)
        {
            if (_name != name)
            {
                if (validate)
                {
                    NameValidator.ValidateName(name, "field");
                }

                _name = name;
            }
        }

        /// <inheritdoc/>
        public string Description { get; set; }

        /// <inheritdoc/>
        public string DeprecationReason { get; set; }

        /// <summary>
        /// Gets or sets the default value of the field. Only applies to fields of input object graph types.
        /// </summary>
        public object DefaultValue
        {
            get => _defaultValue;
            set
            {
                if (!(ResolvedType?.GetNamedType() is GraphQLTypeReference))
                    _ = value.AstFromValue(null, ResolvedType); // HACK: https://github.com/graphql-dotnet/graphql-dotnet/issues/1795

                _defaultValue = value;
                _defaultValueAST = null;
            }
        }

        private Type _type;
        /// <summary>
        /// Gets or sets the graph type of this field.
        /// </summary>
        public Type Type
        {
            get => _type;
            set
            {
                if (value != null && !value.IsGraphType())
                    ThrowInvalidType(value);
                _type = value;
            }
        }

        private void ThrowInvalidType(Type type) => throw new ArgumentOutOfRangeException("value", $"Type '{type}' is not a graph type");

        /// <summary>
        /// Gets or sets the graph type of this field.
        /// </summary>
        public IGraphType ResolvedType { get; set; }

        /// <inheritdoc/>
        public QueryArguments Arguments { get; set; }

        /// <summary>
        /// Gets or sets a field resolver for the field. Only applicable to fields of output graph types.
        /// </summary>
        public IFieldResolver Resolver { get; set; }

        internal IValue GetDefaultValueAST(ISchema schema)
        {
            if (_defaultValueAST == null && _defaultValue != null)
                _defaultValueAST = _defaultValue.AstFromValue(schema, ResolvedType);

            return _defaultValueAST;
        }
    }
}
