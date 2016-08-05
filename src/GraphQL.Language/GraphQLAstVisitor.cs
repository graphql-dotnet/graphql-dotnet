using System;
using System.Collections.Generic;
using GraphQL.Language.AST;

namespace GraphQL.Language
{
    public class GraphQLAstVisitor
    {
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

        public virtual GraphQLValue<bool> BeginVisitBooleanValue(GraphQLValue<bool> value)
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

        public virtual GraphQLValue<string> BeginVisitEnumValue(GraphQLValue<string> value)
        {
            return value;
        }

        public virtual GraphQLFieldSelection BeginVisitFieldSelection(GraphQLFieldSelection selection)
        {
            this.BeginVisitNode(selection.Name);

            if (selection.Alias != null)
                this.BeginVisitAlias((GraphQLName)this.BeginVisitNode(selection.Alias));

            if (selection.SelectionSet != null)
                this.BeginVisitNode(selection.SelectionSet);

            if (selection.Arguments != null)
                this.BeginVisitArguments(selection.Arguments);

            return this.EndVisitFieldSelection(selection);
        }

        public virtual GraphQLValue<float> BeginVisitFloatValue(GraphQLValue<float> value)
        {
            return value;
        }

        public virtual GraphQLFragmentDefinition BeginVisitFragmentDefinition(GraphQLFragmentDefinition node)
        {
            this.BeginVisitNode(node.TypeCondition);
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

        public virtual GraphQLValue<int> BeginVisitIntValue(GraphQLValue<int> value)
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
                case ASTNodeKind.Field: return this.BeginVisitFieldSelection((GraphQLFieldSelection)node);
                case ASTNodeKind.Name: return this.BeginVisitName((GraphQLName)node);
                case ASTNodeKind.Argument: return this.BeginVisitArgument((GraphQLArgument)node);
                case ASTNodeKind.FragmentSpread: return this.BeginVisitFragmentSpread((GraphQLFragmentSpread)node);
                case ASTNodeKind.FragmentDefinition: return this.BeginVisitFragmentDefinition((GraphQLFragmentDefinition)node);
                case ASTNodeKind.InlineFragment: return this.BeginVisitInlineFragment((GraphQLInlineFragment)node);
                case ASTNodeKind.NamedType: return this.BeginVisitNamedType((GraphQLNamedType)node);
                case ASTNodeKind.Directive: return this.BeginVisitDirective((GraphQLDirective)node);
                case ASTNodeKind.Variable: return this.BeginVisitVariable((GraphQLVariable)node);
                case ASTNodeKind.IntValue: return this.BeginVisitIntValue((GraphQLValue<int>)node);
                case ASTNodeKind.FloatValue: return this.BeginVisitFloatValue((GraphQLValue<float>)node);
                case ASTNodeKind.StringValue: return this.BeginVisitStringValue((GraphQLValue<string>)node);
                case ASTNodeKind.BooleanValue: return this.BeginVisitBooleanValue((GraphQLValue<bool>)node);
                case ASTNodeKind.EnumValue: return this.BeginVisitEnumValue((GraphQLValue<string>)node);
            }

            throw new NotImplementedException();
        }

        public virtual GraphQLOperationDefinition BeginVisitOperationDefinition(GraphQLOperationDefinition definition)
        {
            if (definition.Name != null)
                this.BeginVisitNode(definition.Name);

            this.BeginVisitNode(definition.SelectionSet);
            return definition;
        }

        public virtual GraphQLSelectionSet BeginVisitSelectionSet(GraphQLSelectionSet selectionSet)
        {
            foreach (var selection in selectionSet.Selections)
                this.BeginVisitNode(selection);

            return selectionSet;
        }

        public virtual GraphQLValue<string> BeginVisitStringValue(GraphQLValue<string> value)
        {
            return value;
        }

        public virtual GraphQLVariable BeginVisitVariable(GraphQLVariable variable)
        {
            if (variable.Name != null)
                this.BeginVisitNode(variable.Name);

            return this.EndVisitVariable(variable);
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
                this.BeginVisitNode(definition);
        }
    }
}
