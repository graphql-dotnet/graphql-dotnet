using System.Collections.Generic;
using System.Linq;
using GraphQL.Introspection;
using GraphQL.Language;
using GraphQL.Types;

namespace GraphQL.Validation
{
    public class TypeInfo : INodeVisitor
    {
        private readonly ISchema _schema;
        private readonly Stack<GraphType> _typeStack = new Stack<GraphType>();
        private readonly Stack<GraphType> _inputTypeStack = new Stack<GraphType>();
        private readonly Stack<GraphType> _parentTypeStack = new Stack<GraphType>();
        private readonly Stack<FieldType> _fieldDefStack = new Stack<FieldType>();
        private DirectiveGraphType _directive;
        private QueryArgument _argument;

        public TypeInfo(ISchema schema)
        {
            _schema = schema;
        }

        public GraphType GetLastType()
        {
            return _typeStack.Any() ? _typeStack.Peek() : null;
        }

        public GraphType GetInputType()
        {
            return _inputTypeStack.Any() ? _inputTypeStack.Peek() : null;
        }

        public GraphType GetParentType()
        {
            return _parentTypeStack.Any() ? _parentTypeStack.Peek() : null;
        }

        public FieldType GetFieldDef()
        {
            return _fieldDefStack.Any() ? _fieldDefStack.Peek() : null;
        }

        public DirectiveGraphType GetDirective()
        {
            return _directive;
        }

        public QueryArgument GetArgument()
        {
            return _argument;
        }

        public void Enter(INode node)
        {
            if (node is SelectionSet)
            {
                _parentTypeStack.Push(GetLastType());
                return;
            }

            if (node is Field)
            {
                var field = (Field) node;
                var parentType = _parentTypeStack.Peek();
                var fieldType = GetFieldDef(_schema, parentType, field);
                _fieldDefStack.Push(fieldType);
                var targetType = _schema.FindType(fieldType?.Type);
                _typeStack.Push(targetType);
                return;
            }

            if (node is Directive)
            {
                var directive = (Directive) node;
                _directive = _schema.Directives.SingleOrDefault(x => x.Name == directive.Name);
            }

            if (node is Operation)
            {
                var op = (Operation) node;
                GraphType type = null;
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
                }
                _typeStack.Push(type);
                return;
            }

            if (node is FragmentDefinition)
            {
                var def = (FragmentDefinition) node;
                var type = _schema.FindType(def.Type.Name);
                _typeStack.Push(type);
                return;
            }

            if (node is InlineFragment)
            {
                var def = (InlineFragment) node;
                var type = _schema.FindType(def.Type.Name);
                _typeStack.Push(type);
                return;
            }

            if (node is VariableDefinition)
            {
                var varDef = (VariableDefinition) node;
                var inputType = varDef.Type.GraphTypeFromType(_schema);
                _inputTypeStack.Push(inputType);
                return;
            }

            if (node is Argument)
            {
                var argAst = (Argument) node;
                QueryArgument argDef = null;
                GraphType argType = null;

                var args = GetDirective() != null ? GetDirective()?.Arguments : GetFieldDef()?.Arguments;

                if (args != null)
                {
                    argDef = args.Find(argAst.Name);
                    argType = _schema.FindType(argDef?.Type);
                }

                _argument = argDef;
                _inputTypeStack.Push(argType);
            }
        }

        public void Leave(INode node)
        {
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
        }

        private FieldType GetFieldDef(ISchema schema, GraphType parentType, Field field)
        {
            var name = field.Name;

            if (name == SchemaIntrospection.SchemaMeta.Name
                && Equals(schema.Query, parentType))
            {
                return SchemaIntrospection.SchemaMeta;
            }

            if (name == SchemaIntrospection.TypeMeta.Name
                && Equals(schema.Query, parentType))
            {
                return SchemaIntrospection.TypeMeta;
            }

            if (name == SchemaIntrospection.TypeNameMeta.Name
                && (parentType is ObjectGraphType
                    || parentType is InterfaceGraphType
                    || parentType is UnionGraphType))
            {
                return SchemaIntrospection.TypeNameMeta;
            }

            if (parentType is ObjectGraphType || parentType is InterfaceGraphType)
            {
                return parentType.Fields.FirstOrDefault(x => x.Name == field.Name);
            }

            return null;
        }
    }
}
