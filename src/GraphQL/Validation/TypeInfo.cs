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
        private readonly Stack<GraphType> _parentTypeStack = new Stack<GraphType>();
        private readonly Stack<FieldType> _fieldDefStack = new Stack<FieldType>();

        public TypeInfo(ISchema schema)
        {
            _schema = schema;
        }

        public GraphType GetLastType()
        {
            return _typeStack.Any() ? _typeStack.Peek() : null;
        }

        public GraphType GetParentType()
        {
            return _parentTypeStack.Any() ? _parentTypeStack.Peek() : null;
        }

        public FieldType GetFieldDef()
        {
            return _fieldDefStack.Any() ? _fieldDefStack.Peek() : null;
        }

        public void Enter(INode node)
        {
//            System.Diagnostics.Debug.WriteLine($"Entering: {node}");

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
        }

        public void Leave(INode node)
        {
//            System.Diagnostics.Debug.WriteLine($"Leaving: {node}");

            if (node is Operation
                || node is FragmentDefinition)
            {
                _typeStack.Pop();
            }

            if (node is SelectionSet)
            {
                _parentTypeStack.Pop();
            }

            if (node is Field)
            {
                _fieldDefStack.Pop();
                _typeStack.Pop();
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
