using GraphQL.Resolvers;
using System;
using System.Diagnostics;
using GraphQL.Utilities;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    [DebuggerDisplay("{Name,nq}: {ResolvedType,nq}")]
    public class FieldType : MetadataProvider, IFieldType
    {
        private object _defaultValue;
        private IValue _defaultValueAST;

        public string Name { get; set; }

        public string Description { get; set; }

        public string DeprecationReason { get; set; }

        public object DefaultValue
        {
            get => _defaultValue;
            set
            {
                _defaultValue = value;
                _defaultValueAST = null;
            }
        }

        public Type Type { get; set; }

        public IGraphType ResolvedType { get; set; }

        public QueryArguments Arguments { get; set; }

        public IFieldResolver Resolver { get; set; }

        internal IValue GetDefaultValueAST(ISchema schema)
        {
            if (_defaultValueAST == null && _defaultValue != null)
                _defaultValueAST = _defaultValue.AstFromValue(schema, ResolvedType);

            return _defaultValueAST;
        }
    }
}
