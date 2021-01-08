using System.Collections.Generic;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation
{
    /// <summary>
    /// Provides information pertaining to the current state of the AST tree while being walked.
    /// </summary>
    public class TypeInfo : INodeVisitor
    {
        private readonly ISchema _schema;
        private readonly Stack<IGraphType> _typeStack = new Stack<IGraphType>();
        private readonly Stack<IGraphType> _inputTypeStack = new Stack<IGraphType>();
        private readonly Stack<IGraphType> _parentTypeStack = new Stack<IGraphType>();
        private readonly Stack<FieldType> _fieldDefStack = new Stack<FieldType>();
        private readonly Stack<INode> _ancestorStack = new Stack<INode>();
        private DirectiveGraphType _directive;
        private QueryArgument _argument;

        /// <summary>
        /// Initializes a new instance for the specified schema.
        /// </summary>
        /// <param name="schema"></param>
        public TypeInfo(ISchema schema)
        {
            _schema = schema;
        }

        /// <summary>
        /// Returns an ancestor of the current node.
        /// </summary>
        /// <param name="index">Index of the ancestor; 0 for the node itself, 1 for the direct ancestor and so on.</param>
        public INode GetAncestor(int index)
        {
            var e = _ancestorStack.GetEnumerator();

            int i = index;
            do
            {
                _ = e.MoveNext();
            }
            while (i-- > 0);

            return e.Current; // throws if index is out of range
        }

        /// <summary>
        /// Returns the last graph type matched, or null if none.
        /// </summary>
        public IGraphType GetLastType()
        {
            return _typeStack.Count > 0 ? _typeStack.Peek() : null;
        }

        /// <summary>
        /// Returns the last input graph type matched, or null if none.
        /// </summary>
        public IGraphType GetInputType()
        {
            return _inputTypeStack.Count > 0 ? _inputTypeStack.Peek() : null;
        }

        /// <summary>
        /// Returns the parent graph type of the current node, or null if none.
        /// </summary>
        public IGraphType GetParentType()
        {
            return _parentTypeStack.Count > 0 ? _parentTypeStack.Peek() : null;
        }

        /// <summary>
        /// Returns the last field type matched, or null if none.
        /// </summary>
        public FieldType GetFieldDef()
        {
            return _fieldDefStack.Count > 0 ? _fieldDefStack.Peek() : null;
        }

        /// <summary>
        /// Returns the last directive specified, or null if none.
        /// </summary>
        /// <returns></returns>
        public DirectiveGraphType GetDirective()
        {
            return _directive;
        }

        /// <summary>
        /// Returns the last query argument matched, or null if none.
        /// </summary>
        /// <returns></returns>
        public QueryArgument GetArgument()
        {
            return _argument;
        }

        /// <inheritdoc/>
        public void Enter(INode node)
        {
            _ancestorStack.Push(node);

            if (node is SelectionSet)
            {
                _parentTypeStack.Push(GetLastType());
                return;
            }

            if (node is Field field)
            {
                var parentType = _parentTypeStack.Peek().GetNamedType();
                var fieldType = GetFieldDef(_schema, parentType, field);
                _fieldDefStack.Push(fieldType);
                var targetType = fieldType?.ResolvedType;
                _typeStack.Push(targetType);
                return;
            }

            if (node is Directive directive)
            {
                _directive = _schema.FindDirective(directive.Name);
            }

            if (node is Operation op)
            {
                IGraphType type = null;
                if (op.OperationType == OperationType.Query)
                {
                    type = _schema.Query;
                }
                else if (op.OperationType == OperationType.Mutation)
                {
                    type = _schema.Mutation;
                }
                else if (op.OperationType == OperationType.Subscription)
                {
                    type = _schema.Subscription;
                }
                _typeStack.Push(type);
                return;
            }

            if (node is FragmentDefinition def1)
            {
                var type = _schema.FindType(def1.Type.Name);
                _typeStack.Push(type);
                return;
            }

            if (node is InlineFragment def)
            {
                var type = def.Type != null ? _schema.FindType(def.Type.Name) : GetLastType();
                _typeStack.Push(type);
                return;
            }

            if (node is VariableDefinition varDef)
            {
                var inputType = varDef.Type.GraphTypeFromType(_schema);
                _inputTypeStack.Push(inputType);
                return;
            }

            if (node is Argument argAst)
            {
                QueryArgument argDef = null;
                IGraphType argType = null;

                var args = GetDirective() != null ? GetDirective()?.Arguments : GetFieldDef()?.Arguments;

                if (args != null)
                {
                    argDef = args.Find(argAst.Name);
                    argType = argDef?.ResolvedType;
                }

                _argument = argDef;
                _inputTypeStack.Push(argType);
            }

            if (node is ListValue)
            {
                var type = GetInputType().GetNamedType();
                _inputTypeStack.Push(type);
            }

            if (node is ObjectField objectField)
            {
                var objectType = GetInputType().GetNamedType();
                IGraphType fieldType = null;

                if (objectType is IInputObjectGraphType complexType)
                {
                    var inputField = complexType.GetField(objectField.Name);

                    fieldType = inputField?.ResolvedType;
                }

                _inputTypeStack.Push(fieldType);
            }
        }

        /// <inheritdoc/>
        public void Leave(INode node)
        {
            _ancestorStack.Pop();

            if (node is SelectionSet)
            {
                _parentTypeStack.Pop();
                return;
            }

            if (node is Field)
            {
                _fieldDefStack.Pop();
                _typeStack.Pop();
                return;
            }

            if (node is Directive)
            {
                _directive = null;
                return;
            }

            if (node is Operation
                || node is FragmentDefinition
                || node is InlineFragment)
            {
                _typeStack.Pop();
                return;
            }

            if (node is VariableDefinition)
            {
                _inputTypeStack.Pop();
                return;
            }

            if (node is Argument)
            {
                _argument = null;
                _inputTypeStack.Pop();
                return;
            }

            if (node is ListValue || node is ObjectField)
            {
                _inputTypeStack.Pop();
                return;
            }
        }

        private FieldType GetFieldDef(ISchema schema, IGraphType parentType, Field field)
        {
            var name = field.Name;

            if (name == schema.SchemaMetaFieldType.Name
                && Equals(schema.Query, parentType))
            {
                return schema.SchemaMetaFieldType;
            }

            if (name == schema.TypeMetaFieldType.Name
                && Equals(schema.Query, parentType))
            {
                return schema.TypeMetaFieldType;
            }

            if (name == schema.TypeNameMetaFieldType.Name && parentType.IsCompositeType())
            {
                return schema.TypeNameMetaFieldType;
            }

            if (parentType is IObjectGraphType || parentType is IInterfaceGraphType)
            {
                var complexType = (IComplexGraphType)parentType;

                return complexType.GetField(field.Name);
            }

            return null;
        }
    }
}
