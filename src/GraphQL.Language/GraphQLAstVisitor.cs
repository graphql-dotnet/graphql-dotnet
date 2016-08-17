using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Language
{
    public class GraphQLAstVisitor
    {
        protected IDictionary<string, GraphQLFragmentDefinition> Fragments { get; private set; }

        public GraphQLAstVisitor()
        {
            this.Fragments = new Dictionary<string, GraphQLFragmentDefinition>();
        }

        public virtual GraphQLName BeginVisitAlias(GraphQLName alias)
        {
            return alias;
        }

        public virtual GraphQLArgument BeginVisitArgument(GraphQLArgument argument)
        {
            if (argument.Name != null)
                this.BeginVisitNode(argument.Name);

            if (argument.Value != null)
                this.BeginVisitNode(argument.Value);

            return this.EndVisitArgument(argument);
        }

        public virtual IEnumerable<GraphQLArgument> BeginVisitArguments(IEnumerable<GraphQLArgument> arguments)
        {
            foreach (var argument in arguments)
                this.BeginVisitNode(argument);

            return arguments;
        }

        public virtual GraphQLScalarValue BeginVisitBooleanValue(GraphQLScalarValue value)
        {
            return value;
        }

        public virtual GraphQLDirective BeginVisitDirective(GraphQLDirective directive)
        {
            if (directive.Name != null)
                this.BeginVisitNode(directive.Name);

            if (directive.Arguments != null)
                this.BeginVisitArguments(directive.Arguments);

            return directive;
        }

        public virtual IEnumerable<GraphQLDirective> BeginVisitDirectives(IEnumerable<GraphQLDirective> directives)
        {
            foreach (var directive in directives)
                this.BeginVisitNode(directive);

            return directives;
        }

        public virtual GraphQLScalarValue BeginVisitEnumValue(GraphQLScalarValue value)
        {
            return value;
        }

        public virtual GraphQLFieldSelection BeginVisitFieldSelection(GraphQLFieldSelection selection)
        {
            this.BeginVisitNode(selection.Name);

            if (selection.Alias != null)
                this.BeginVisitAlias((GraphQLName)this.BeginVisitNode(selection.Alias));

            if (selection.Arguments != null)
                this.BeginVisitArguments(selection.Arguments);

            if (selection.SelectionSet != null)
                this.BeginVisitNode(selection.SelectionSet);

            if (selection.Directives != null)
                this.BeginVisitDirectives(selection.Directives);

            return this.EndVisitFieldSelection(selection);
        }

        public virtual GraphQLScalarValue BeginVisitFloatValue(GraphQLScalarValue value)
        {
            return value;
        }

        public virtual GraphQLFragmentDefinition BeginVisitFragmentDefinition(GraphQLFragmentDefinition node)
        {
            this.BeginVisitNode(node.TypeCondition);
            this.BeginVisitNode(node.Name);

            if (node.SelectionSet != null)
                this.BeginVisitNode(node.SelectionSet);

            return node;
        }

        public virtual GraphQLFragmentSpread BeginVisitFragmentSpread(GraphQLFragmentSpread fragmentSpread)
        {
            this.BeginVisitNode(fragmentSpread.Name);
            return fragmentSpread;
        }

        public virtual GraphQLInlineFragment BeginVisitInlineFragment(GraphQLInlineFragment inlineFragment)
        {
            if (inlineFragment.TypeCondition != null)
                this.BeginVisitNode(inlineFragment.TypeCondition);

            if (inlineFragment.Directives != null)
                this.BeginVisitDirectives(inlineFragment.Directives);

            if (inlineFragment.SelectionSet != null)
                this.BeginVisitSelectionSet(inlineFragment.SelectionSet);

            return inlineFragment;
        }

        public virtual GraphQLScalarValue BeginVisitIntValue(GraphQLScalarValue value)
        {
            return value;
        }

        public virtual GraphQLName BeginVisitName(GraphQLName name)
        {
            return name;
        }

        public virtual GraphQLNamedType BeginVisitNamedType(GraphQLNamedType typeCondition)
        {
            return typeCondition;
        }

        public virtual ASTNode BeginVisitNode(ASTNode node)
        {
            switch (node.Kind)
            {
                case ASTNodeKind.OperationDefinition: return this.BeginVisitOperationDefinition((GraphQLOperationDefinition)node);
                case ASTNodeKind.SelectionSet: return this.BeginVisitSelectionSet((GraphQLSelectionSet)node);
                case ASTNodeKind.Field: return this.BeginVisitNonIntrospectionFieldSelection((GraphQLFieldSelection)node);
                case ASTNodeKind.Name: return this.BeginVisitName((GraphQLName)node);
                case ASTNodeKind.Argument: return this.BeginVisitArgument((GraphQLArgument)node);
                case ASTNodeKind.FragmentSpread: return this.BeginVisitFragmentSpread((GraphQLFragmentSpread)node);
                case ASTNodeKind.FragmentDefinition: return this.BeginVisitFragmentDefinition((GraphQLFragmentDefinition)node);
                case ASTNodeKind.InlineFragment: return this.BeginVisitInlineFragment((GraphQLInlineFragment)node);
                case ASTNodeKind.NamedType: return this.BeginVisitNamedType((GraphQLNamedType)node);
                case ASTNodeKind.Directive: return this.BeginVisitDirective((GraphQLDirective)node);
                case ASTNodeKind.Variable: return this.BeginVisitVariable((GraphQLVariable)node);
                case ASTNodeKind.IntValue: return this.BeginVisitIntValue((GraphQLScalarValue)node);
                case ASTNodeKind.FloatValue: return this.BeginVisitFloatValue((GraphQLScalarValue)node);
                case ASTNodeKind.StringValue: return this.BeginVisitStringValue((GraphQLScalarValue)node);
                case ASTNodeKind.BooleanValue: return this.BeginVisitBooleanValue((GraphQLScalarValue)node);
                case ASTNodeKind.EnumValue: return this.BeginVisitEnumValue((GraphQLScalarValue)node);
                case ASTNodeKind.ListValue: return this.BeginVisitListValue((GraphQLListValue)node);
                case ASTNodeKind.ObjectValue: return this.BeginVisitObjectValue((GraphQLObjectValue)node);
                case ASTNodeKind.ObjectField: return this.BeginVisitObjectField((GraphQLObjectField)node);
                case ASTNodeKind.VariableDefinition: return this.BeginVisitVariableDefinition((GraphQLVariableDefinition)node);
            }

            return null;
        }

        public virtual GraphQLOperationDefinition BeginVisitOperationDefinition(GraphQLOperationDefinition definition)
        {
            if (definition.Name != null)
                this.BeginVisitNode(definition.Name);

            if (definition.VariableDefinitions != null)
                this.BeginVisitVariableDefinitions(definition.VariableDefinitions);

            this.BeginVisitNode(definition.SelectionSet);

            return this.EndVisitOperationDefinition(definition);
        }

        public virtual GraphQLOperationDefinition EndVisitOperationDefinition(GraphQLOperationDefinition definition)
        {
            return definition;
        }

        public virtual GraphQLSelectionSet BeginVisitSelectionSet(GraphQLSelectionSet selectionSet)
        {
            foreach (var selection in selectionSet.Selections)
                this.BeginVisitNode(selection);

            return selectionSet;
        }

        public virtual GraphQLScalarValue BeginVisitStringValue(GraphQLScalarValue value)
        {
            return value;
        }

        public virtual GraphQLVariable BeginVisitVariable(GraphQLVariable variable)
        {
            if (variable.Name != null)
                this.BeginVisitNode(variable.Name);

            return this.EndVisitVariable(variable);
        }

        public virtual GraphQLVariableDefinition BeginVisitVariableDefinition(GraphQLVariableDefinition node)
        {
            this.BeginVisitNode(node.Type);

            return node;
        }

        public virtual IEnumerable<GraphQLVariableDefinition> BeginVisitVariableDefinitions(
            IEnumerable<GraphQLVariableDefinition> variableDefinitions)
        {
            foreach (var definition in variableDefinitions)
                this.BeginVisitNode(definition);

            return variableDefinitions;
        }

        public virtual GraphQLArgument EndVisitArgument(GraphQLArgument argument)
        {
            return argument;
        }

        public virtual GraphQLFieldSelection EndVisitFieldSelection(GraphQLFieldSelection selection)
        {
            return selection;
        }

        public virtual GraphQLVariable EndVisitVariable(GraphQLVariable variable)
        {
            return variable;
        }

        public virtual void Visit(GraphQLDocument ast)
        {
            foreach (var definition in ast.Definitions)
            {
                if (definition.Kind == ASTNodeKind.FragmentDefinition)
                {
                    var fragment = (GraphQLFragmentDefinition)definition;
                    this.Fragments.Add(fragment.Name.Value, fragment);
                }
            }

            foreach (var definition in ast.Definitions)
            {
                this.BeginVisitNode(definition);
            }
        }

        public virtual GraphQLObjectField BeginVisitObjectField(GraphQLObjectField node)
        {
            this.BeginVisitNode(node.Name);

            this.BeginVisitNode(node.Value);

            return node;
        }

        public virtual GraphQLObjectValue BeginVisitObjectValue(GraphQLObjectValue node)
        {
            foreach (var field in node.Fields)
                this.BeginVisitNode(field);

            return this.EndVisitObjectValue(node);
        }

        public virtual GraphQLObjectValue EndVisitObjectValue(GraphQLObjectValue node)
        {
            return node;
        }

        public virtual GraphQLListValue EndVisitListValue(GraphQLListValue node)
        {
            return node;
        }

        private ASTNode BeginVisitListValue(GraphQLListValue node)
        {
            foreach (var value in node.Values)
                this.BeginVisitNode(value);

            return this.EndVisitListValue(node);
        }

        private ASTNode BeginVisitNonIntrospectionFieldSelection(GraphQLFieldSelection selection)
        {
            return this.BeginVisitFieldSelection(selection);
        }
    }
}
