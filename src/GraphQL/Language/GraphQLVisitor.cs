using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using GraphQL.Parsing;

namespace GraphQL.Language
{
    public class GraphQLVisitor : GraphQLBaseVisitor<object>
    {
        public override object VisitDefaultValue(GraphQLParser.DefaultValueContext context)
        {
            return Visit(context.value());
        }

        public override object VisitValue(GraphQLParser.ValueContext context)
        {
            if (context.STRING() != null)
            {
                return context.STRING().GetText();
            }

            if (context.NUMBER() != null)
            {
                return context.NUMBER().GetText();
            }

            if (context.BOOLEAN() != null)
            {
                return context.BOOLEAN().GetText();
            }

            if (context.@enum() != null)
            {
                return context.@enum().GetText();
            }

            if (context.array() != null)
            {
                return Visit(context.array());
            }

            if (context.@object() != null)
            {
                return Visit(context.@object());
            }

            return null;
        }

        public override object VisitValueOrVariable(GraphQLParser.ValueOrVariableContext context)
        {
            if (context.STRING() != null)
            {
                return context.STRING().GetText();
            }

            if (context.NUMBER() != null)
            {
                return context.NUMBER().GetText();
            }

            if (context.BOOLEAN() != null)
            {
                return context.BOOLEAN().GetText();
            }

            if (context.@enum() != null)
            {
                return context.@enum().GetText();
            }

            if (context.variable() != null)
            {
                return Visit(context.variable());
            }

            if (context.arrayWithVariable() != null)
            {
                return Visit(context.arrayWithVariable());
            }

            if (context.objectWithVariable() != null)
            {
                return Visit(context.objectWithVariable());
            }

            return null;
        }

        public override object VisitVariable(GraphQLParser.VariableContext context)
        {
            var variable = new Variable();
            variable.Name = context.NAME().GetText();
            return variable;
        }

        public override object VisitEnum(GraphQLParser.EnumContext context)
        {
            return context.NAME().GetText();
        }

        public override object VisitArray(GraphQLParser.ArrayContext context)
        {
            return context.value().Select(Visit).ToList();
        }

        public override object VisitArrayWithVariable(GraphQLParser.ArrayWithVariableContext context)
        {
            return context.valueOrVariable().Select(Visit).ToList();
        }

        public override object VisitObject(GraphQLParser.ObjectContext context)
        {
            var obj = new Dictionary<string, object>();

            foreach (var item in context.pair())
            {
                var name = item.NAME().GetText();
                var val = Visit(item.value());
                obj[name] = val;
            }

            return obj;
        }

        public override object VisitObjectWithVariable(GraphQLParser.ObjectWithVariableContext context)
        {
            var obj = new Dictionary<string, object>();

            foreach (var item in context.pairWithVariable())
            {
                var name = item.NAME().GetText();
                var val = Visit(item.valueOrVariable());
                obj[name] = val;
            }

            return obj;
        }

        public override object VisitVariableDefinitions(GraphQLParser.VariableDefinitionsContext context)
        {
            var variables = new Variables();

            context.variableDefinition().Apply(childContext =>
            {
                var variable = Visit(childContext) as Variable;
                variables.Add(variable);
            });

            return variables;
        }

        public override object VisitVariableDefinition(GraphQLParser.VariableDefinitionContext context)
        {
            var variable = new Variable();
            variable.Name = context.variable().GetText().Substring(1);

            if (context.defaultValue() != null)
            {
                variable.DefaultValue = Visit(context.defaultValue());
            }

            if (context.type() != null)
            {
                variable.Type = Visit(context.type()) as VariableType;
            }

            return variable;
        }

        public override object VisitType(GraphQLParser.TypeContext context)
        {
            var type = new VariableType();

            if (context.typeName() != null)
            {
                type.Name = context.typeName().GetText();
            }

            if (context.listType() != null)
            {
                type.IsList = true;
                type.Name = context.listType().type().typeName().GetText();
            }

            if (context.nonNullType() != null)
            {
                type.AllowsNull = false;
            }

            return type;
        }

        public override object VisitTypeName(GraphQLParser.TypeNameContext context)
        {
            return context.GetText();
        }

        public override object VisitArguments(GraphQLParser.ArgumentsContext context)
        {
            var arguments = new Arguments();

            foreach (var arg in context.argument())
            {
                arguments.Add((Argument) Visit(arg));
            }

            return arguments;
        }

        public override object VisitArgument(GraphQLParser.ArgumentContext context)
        {
            var name = context.NAME().GetText();
            var valOrVar = context.valueOrVariable();
            var val = Visit(valOrVar);
            //var val = valOrVar.value() != null ? valOrVar.value().GetText() : valOrVar.variable().GetText();
            var arg = new Argument
            {
                Name = name,
                Value = val
            };

            return arg;
        }

        public override object VisitDirectives(GraphQLParser.DirectivesContext context)
        {
            var directives = new Directives();

            foreach (var dir in context.directive())
            {
                directives.Add((Directive)Visit(dir));
            }

            return directives;
        }

        public override object VisitDirective(GraphQLParser.DirectiveContext context)
        {
            var directive = new Directive();
            directive.Name = context.NAME().GetText();

            if (context.arguments() != null)
            {
                directive.Arguments = Visit(context.arguments()) as Arguments;
            }

            return directive;
        }

        public override object VisitFragmentSpread(GraphQLParser.FragmentSpreadContext context)
        {
            var fragment = new FragmentSpread();
            fragment.Name = context.fragmentName().GetText();

            if (context.directives() != null)
            {
                fragment.Directives = Visit(context.directives()) as Directives;
            }

            return fragment;
        }

        public override object VisitInlineFragment(GraphQLParser.InlineFragmentContext context)
        {
            var fragment = new InlineFragment();

            fragment.Type = context.typeCondition().typeName().NAME().GetText();

            if (context.directives() != null)
            {
                fragment.Directives = Visit(context.directives()) as Directives;
            }

            if (context.selectionSet() != null)
            {
                fragment.Selections = Visit(context.selectionSet()) as Selections;
            }

            return fragment;
        }

        public override object VisitFragmentDefinition(GraphQLParser.FragmentDefinitionContext context)
        {
            var name = context.fragmentName().GetText();
            var type = context.typeCondition().typeName().NAME().GetText();

            var fragmentDefinition = new FragmentDefinition();
            fragmentDefinition.Name = name;
            fragmentDefinition.Type = type;

            var directiveContext = context.directives();
            if (directiveContext != null)
            {
                var directives = Visit(directiveContext) as Directives;
                fragmentDefinition.Directives = directives;
            }

            var selectionContext = context.selectionSet();
            if (selectionContext != null)
            {
                var selections = Visit(selectionContext) as Selections;
                fragmentDefinition.Selections = selections;
            }

            return fragmentDefinition;
        }

        public override object VisitField(GraphQLParser.FieldContext context)
        {
            var field = new Field();

            if (context.fieldName().alias() != null)
            {
                var aliasContext = context.fieldName().alias();
                field.Alias = aliasContext.NAME(0).GetText();
                field.Name = aliasContext.NAME(1).GetText();
            }
            else
            {
                field.Name = context.fieldName().NAME().GetText();
            }


            if (context.arguments() != null)
            {
                field.Arguments = Visit(context.arguments()) as Arguments;
            }

            if (context.directives() != null)
            {
                field.Directives = Visit(context.directives()) as Directives;
            }

            if (context.selectionSet() != null)
            {
                field.Selections = Visit(context.selectionSet()) as Selections;
            }

            return field;
        }

        public override object VisitSelectionSet(GraphQLParser.SelectionSetContext context)
        {
            var selections = new Selections();

            foreach (var selectionContext in context.selection())
            {
                var selection = Visit(selectionContext) as Selection;
                selections.Add(selection);
            }

            return selections;
        }

        public override object VisitSelection(GraphQLParser.SelectionContext context)
        {
            var selection = new Selection();

            if (context.field() != null)
            {
                var field = Visit(context.field()) as Field;
                selection.Field = field;
            }

            if (context.fragmentSpread() != null)
            {
                var fragment = Visit(context.fragmentSpread()) as IFragment;
                selection.Fragment = fragment;
            }

            if (context.inlineFragment() != null)
            {
                var fragment = Visit(context.inlineFragment()) as IFragment;
                selection.Fragment = fragment;
            }

            return selection;
        }

        public override object VisitOperationDefinition(GraphQLParser.OperationDefinitionContext context)
        {
            var operation = new Operation();

            operation.Name = context.NAME() != null ? context.NAME().GetText() : "";
            operation.Selections = Visit(context.selectionSet()) as Selections;

            if (context.operationType() != null)
            {
                operation.OperationType = (OperationType)Enum.Parse(typeof(OperationType), context.operationType().GetText(), true);
            }

            if (context.variableDefinitions() != null)
            {
                operation.Variables = Visit(context.variableDefinitions()) as Variables;
            }

            if (context.directives() != null)
            {
                operation.Directives = Visit(context.directives()) as Directives;
            }

            return operation;
        }

        public override object VisitDocument(GraphQLParser.DocumentContext context)
        {
            var document = new Document();

            context.definition().Apply(childContext =>
            {
                var definition = Visit(childContext);

                if (definition is IFragment)
                {
                    document.Fragments.Add(definition as IFragment);
                }
                else if (definition is Operation)
                {
                    document.Operations.Add(definition as Operation);
                }
                else
                {
                    throw new Exception("Unhandled document definition");
                }
            });

            return document;
        }
    }
}
