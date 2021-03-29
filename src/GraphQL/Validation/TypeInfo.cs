using System;
using System.Collections.Generic;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Validation
{
    /// <summary>
    /// Provides information pertaining to the current state of the AST tree while being walked.
    /// Thus, validation rules checking is designed for sequential execution.
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

        private static T PeekElement<T>(Stack<T> from, int index)
        {
            if (index == 0)
            {
                return from.Count > 0 ? from.Peek() : default;
            }
            else
            {
                if (index >= from.Count)
                    throw new InvalidOperationException($"Stack contains only {from.Count} items");

                var e = from.GetEnumerator();

                int i = index;
                do
                {
                    _ = e.MoveNext();
                }
                while (i-- > 0);

                return e.Current;
            }
        }

        /// <summary>
        /// Returns an ancestor of the current node.
        /// </summary>
        /// <param name="index">Index of the ancestor; 0 for the node itself, 1 for the direct ancestor and so on.</param>
        public INode GetAncestor(int index) => PeekElement(_ancestorStack, index);

        /// <summary>
        /// Returns the last graph type matched, or <see langword="null"/> if none.
        /// </summary>
        /// <param name="index">Index of the type; 0 for the top-most type, 1 for the direct ancestor and so on.</param>
        public IGraphType GetLastType(int index = 0) => PeekElement(_typeStack, index);

        /// <summary>
        /// Returns the last input graph type matched, or <see langword="null"/> if none.
        /// </summary>
        /// <param name="index">Index of the type; 0 for the top-most type, 1 for the direct ancestor and so on.</param>
        public IGraphType GetInputType(int index = 0) => PeekElement(_inputTypeStack, index);

        /// <summary>
        /// Returns the parent graph type of the current node, or <see langword="null"/> if none.
        /// </summary>
        /// <param name="index">Index of the type; 0 for the top-most type, 1 for the direct ancestor and so on.</param>
        public IGraphType GetParentType(int index = 0) => PeekElement(_parentTypeStack, index);

        /// <summary>
        /// Returns the last field type matched, or <see langword="null"/> if none.
        /// </summary>
        /// <param name="index">Index of the field; 0 for the top-most field, 1 for the direct ancestor and so on.</param>
        public FieldType GetFieldDef(int index = 0) => PeekElement(_fieldDefStack, index);

        /// <summary>
        /// Returns the last directive specified, or <see langword="null"/> if none.
        /// </summary>
        public DirectiveGraphType GetDirective() => _directive;

        /// <summary>
        /// Returns the last query argument matched, or <see langword="null"/> if none.
        /// </summary>
        public QueryArgument GetArgument() => _argument;

        /// <inheritdoc/>
        public void Enter(INode node, ValidationContext context)
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
                _directive = _schema.Directives.Find(directive.Name);
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
                var type = _schema.AllTypes[def1.Type.Name];
                _typeStack.Push(type);
                return;
            }

            if (node is InlineFragment def)
            {
                var type = def.Type != null ? _schema.AllTypes[def.Type.Name] : GetLastType();
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
        public void Leave(INode node, ValidationContext context)
        {
            _ancestorStack.Pop();

            if (node is SelectionSet)
            {
                _parentTypeStack.Pop();
            }
            else if (node is Field)
            {
                _fieldDefStack.Pop();
                _typeStack.Pop();
            }
            else if (node is Directive)
            {
                _directive = null;
            }
            else if (node is Operation || node is FragmentDefinition || node is InlineFragment)
            {
                _typeStack.Pop();
            }
            else if (node is VariableDefinition)
            {
                _inputTypeStack.Pop();
            }
            else if (node is Argument)
            {
                _argument = null;
                _inputTypeStack.Pop();
            }
            else if (node is ListValue || node is ObjectField)
            {
                _inputTypeStack.Pop();
            }
        }

        private FieldType GetFieldDef(ISchema schema, IGraphType parentType, Field field)
        {
            var name = field.Name;

            if (name == schema.SchemaMetaFieldType.Name && Equals(schema.Query, parentType))
            {
                return schema.SchemaMetaFieldType;
            }

            if (name == schema.TypeMetaFieldType.Name && Equals(schema.Query, parentType))
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

        /// <summary>
        /// Tracks already visited fragments to maintain O(N) and to ensure that cycles
        /// are not redundantly reported.
        /// </summary>
        internal HashSet<string> NoFragmentCycles_VisitedFrags;
        /// <summary>
        /// Array of AST nodes used to produce meaningful errors
        /// </summary>
        internal Stack<FragmentSpread> NoFragmentCycles_SpreadPath;
        /// <summary>
        /// Position in the spread path
        /// </summary>
        internal Dictionary<string, int> NoFragmentCycles_SpreadPathIndexByName;

        internal HashSet<string> NoUndefinedVariables_VariableNameDefined;

        internal List<Operation> NoUnusedFragments_OperationDefs;
        internal List<FragmentDefinition> NoUnusedFragments_FragmentDefs;

        internal List<VariableDefinition> NoUnusedVariables_VariableDefs;

        internal Dictionary<string, Argument> UniqueArgumentNames_KnownArgs;

        internal Dictionary<string, FragmentDefinition> UniqueFragmentNames_KnownFragments;

        internal Stack<Dictionary<string, IValue>> UniqueInputFieldNames_KnownNameStack;
        internal Dictionary<string, IValue> UniqueInputFieldNames_KnownNames;

        internal HashSet<string> UniqueOperationNames_Frequency;

        internal Dictionary<string, VariableDefinition> UniqueVariableNames_KnownVariables;

        internal Dictionary<string, VariableDefinition> VariablesInAllowedPosition_VarDefMap;
    }
}
