using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using GraphQL.Parsing;

namespace GraphQL.Language.AST
{
    public class GraphQLVisitor : GraphQLBaseVisitor<object>
    {
        public override object VisitDefaultValue(GraphQLParser.DefaultValueContext context)
        {
            return Visit(context.value());
        }

        public override object VisitValue(GraphQLParser.ValueContext context)
        {
            if (context.StringValue() != null)
            {
                var val = new StringValue(context.StringValue().GetText());
                NewNode(val, context);
                return val;
            }

            if (context.IntValue() != null)
            {
                var value = context.IntValue().GetText();
                int intResult;
                if (int.TryParse(value, out intResult))
                {
                    var val = new IntValue(intResult);
                    NewNode(val, context);
                    return val;
                }

                // If the value doesn't fit in an integer, revert to using long...
                long longResult;
                if (long.TryParse(value, out longResult))
                {
                    var val = new LongValue(longResult);
                    NewNode(val, context);
                    return val;
                }
            }

            if (context.FloatValue() != null)
            {
                var val = new FloatValue(double.Parse(context.FloatValue().GetText()));
                NewNode(val, context);
                return val;
            }

            if (context.BooleanValue() != null)
            {
                var val = new BooleanValue(bool.Parse(context.BooleanValue().GetText()));
                NewNode(val, context);
                return val;
            }

            if (context.enumValue() != null)
            {
                var val = new EnumValue(context.enumValue().GetText());
                NewNode(val, context);
                return val;
            }

            if (context.arrayValue() != null)
            {
                return Visit(context.arrayValue());
            }

            if (context.objectValue() != null)
            {
                return Visit(context.objectValue());
            }

            return null;
        }

        public override object VisitValueWithVariable(GraphQLParser.ValueWithVariableContext context)
        {
            if (context.StringValue() != null)
            {
                var val = new StringValue(context.StringValue().GetText());
                NewNode(val, context);
                return val;
            }

            if (context.IntValue() != null)
            {
                var value = context.IntValue().GetText();
                int intResult;
                if (int.TryParse(value, out intResult))
                {
                    var val = new IntValue(intResult);
                    NewNode(val, context);
                    return val;
                }

                // If the value doesn't fit in an integer, revert to using long...
                long longResult;
                if (long.TryParse(value, out longResult))
                {
                    var val = new LongValue(longResult);
                    NewNode(val, context);
                    return val;
                }
            }

            if (context.FloatValue() != null)
            {
                var val = new FloatValue(double.Parse(context.FloatValue().GetText()));
                NewNode(val, context);
                return val;
            }

            if (context.BooleanValue() != null)
            {
                var val = new BooleanValue(bool.Parse(context.BooleanValue().GetText()));
                NewNode(val, context);
                return val;
            }

            if (context.enumValue() != null)
            {
                var val = new EnumValue(context.enumValue().GetText());
                NewNode(val, context);
                return val;
            }

            if (context.variable() != null)
            {
                return Visit(context.variable());
            }

            if (context.arrayValueWithVariable() != null)
            {
                return Visit(context.arrayValueWithVariable());
            }

            if (context.objectValueWithVariable() != null)
            {
                return Visit(context.objectValueWithVariable());
            }

            return null;
        }

        public override object VisitVariable(GraphQLParser.VariableContext context)
        {
            var name = context.NAME().GetText();
            var variable = new VariableReference(new NameNode(name));
            NewNode(variable, context);
            return variable;
        }

        public override object VisitArrayValue(GraphQLParser.ArrayValueContext context)
        {
            var values = context.value().Select(Visit).OfType<IValue>().ToList();
            var list = new ListValue(values);
            NewNode(list, context);
            return list;
        }

        public override object VisitArrayValueWithVariable(GraphQLParser.ArrayValueWithVariableContext context)
        {
            var values = context.valueWithVariable().Select(Visit).OfType<IValue>().ToList();
            var list = new ListValue(values);
            NewNode(list, context);
            return list;
        }

        public override object VisitObjectValue(GraphQLParser.ObjectValueContext context)
        {
            var fields = new List<ObjectField>();

            foreach (var item in context.objectField())
            {
                var name = item.NAME().GetText();
                var val = Visit(item.value()) as IValue;
                var field = new ObjectField(name, val);
                NewNode(field, context);
                fields.Add(field);
            }

            var objValue = new ObjectValue(fields);
            NewNode(objValue, context);

            return objValue;
        }

        public override object VisitObjectValueWithVariable(GraphQLParser.ObjectValueWithVariableContext context)
        {
            var fields = new List<ObjectField>();

            foreach (var item in context.objectFieldWithVariable())
            {
                var name = item.NAME().GetText();
                var val = Visit(item.valueWithVariable()) as IValue;
                var field = new ObjectField(name, val);
                NewNode(field, context);
                fields.Add(field);
            }

            var objValue = new ObjectValue(fields);
            NewNode(objValue, context);

            return objValue;
        }

        public override object VisitVariableDefinitions(GraphQLParser.VariableDefinitionsContext context)
        {
            var variables = new VariableDefinitions();

            context.variableDefinition().Apply(childContext =>
            {
                var variable = Visit(childContext) as VariableDefinition;
                variables.Add(variable);
            });

            return variables;
        }

        public override object VisitVariableDefinition(GraphQLParser.VariableDefinitionContext context)
        {
            var variable = new VariableDefinition();
            NewNode(variable, context);
            variable.Name = context.variable().GetText().Substring(1);

            if (context.defaultValue() != null)
            {
                variable.DefaultValue = Visit(context.defaultValue()) as IValue;
            }

            if (context.type() != null)
            {
                variable.Type = Visit(context.type()) as IType;
            }

            return variable;
        }

        public override object VisitType(GraphQLParser.TypeContext context)
        {
            if (context.typeName() != null)
            {
                return Visit(context.typeName());
            }

            if (context.listType() != null)
            {
                return Visit(context.listType());
            }

            if (context.nonNullType() != null)
            {
                return Visit(context.nonNullType());
            }

            return null;
        }

        public override object VisitTypeName(GraphQLParser.TypeNameContext context)
        {
            var type = new NamedType(context.GetText());
            NewNode(type, context);
            return type;
        }

        public override object VisitListType(GraphQLParser.ListTypeContext context)
        {
            var type = new ListType(Visit(context.type()) as IType);
            NewNode(type, context);
            return type;
        }

        public override object VisitNonNullType(GraphQLParser.NonNullTypeContext context)
        {
            var type = new NonNullType(
                context.typeName() != null
                    ? Visit(context.typeName()) as IType
                    : Visit(context.listType()) as IType
                );

            NewNode(type, context);

            return type;
        }

        public override object VisitArguments(GraphQLParser.ArgumentsContext context)
        {
            var arguments = new Arguments();
            NewNode(arguments, context);

            foreach (var arg in context.argument())
            {
                arguments.Add((Argument) Visit(arg));
            }

            return arguments;
        }

        public override object VisitArgument(GraphQLParser.ArgumentContext context)
        {
            var name = context.NAME().GetText();
            var valOrVar = context.valueWithVariable();
            var val = Visit(valOrVar) as IValue;
            var arg = new Argument
            {
                Name = name,
                Value = val
            };
            NewNode(arg, context);

            return arg;
        }

        public override object VisitDirectives(GraphQLParser.DirectivesContext context)
        {
            var directives = new Directives();
            NewNode(directives, context);

            foreach (var dir in context.directive())
            {
                directives.Add((Directive)Visit(dir));
            }

            return directives;
        }

        public override object VisitDirective(GraphQLParser.DirectiveContext context)
        {
            var directive = new Directive();
            NewNode(directive, context);
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
            NewNode(fragment, context);
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
            NewNode(fragment, context);

            if (context.typeCondition() != null)
            {
                fragment.Type = Visit(context.typeCondition().typeName()) as NamedType;
            }

            if (context.directives() != null)
            {
                fragment.Directives = Visit(context.directives()) as Directives;
            }

            if (context.selectionSet() != null)
            {
                fragment.SelectionSet = Visit(context.selectionSet()) as SelectionSet;
            }

            return fragment;
        }

        public override object VisitFragmentDefinition(GraphQLParser.FragmentDefinitionContext context)
        {
            var name = context.fragmentName().GetText();
            var type = Visit(context.typeCondition().typeName()) as NamedType;

            var fragmentDefinition = new FragmentDefinition();
            NewNode(fragmentDefinition, context);

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
                var selections = Visit(selectionContext) as SelectionSet;
                fragmentDefinition.SelectionSet = selections;
            }

            return fragmentDefinition;
        }

        public override object VisitField(GraphQLParser.FieldContext context)
        {
            var field = new Field();
            NewNode(field, context);

            if (context.NAME() != null)
            {
                field.Name = context.NAME().GetText();

            }

            if (context.alias() != null)
            {
                field.Alias = context.alias().NAME().GetText();
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
                field.SelectionSet = Visit(context.selectionSet()) as SelectionSet;
            }

            return field;
        }

        public override object VisitSelectionSet(GraphQLParser.SelectionSetContext context)
        {
            var selections = new SelectionSet();
            NewNode(selections, context);

            foreach (var selectionContext in context.selection())
            {
                var selection = Visit(selectionContext) as ISelection;
                selections.Add(selection);
            }

            return selections;
        }

        public override object VisitSelection(GraphQLParser.SelectionContext context)
        {
            if (context.field() != null)
            {
                return Visit(context.field());
            }

            if (context.fragmentSpread() != null)
            {
                return Visit(context.fragmentSpread());
            }

            if (context.inlineFragment() != null)
            {
                return Visit(context.inlineFragment());
            }

            return null;
        }

        public override object VisitOperationDefinition(GraphQLParser.OperationDefinitionContext context)
        {
            var operation = new Operation();
            NewNode(operation, context);

            operation.Name = context.NAME() != null ? context.NAME().GetText() : "";
            operation.SelectionSet = Visit(context.selectionSet()) as SelectionSet;

            if (context.operationType() != null)
            {
                operation.OperationType = (OperationType)Enum.Parse(typeof(OperationType), context.operationType().GetText(), true);
            }

            if (context.variableDefinitions() != null)
            {
                operation.Variables = Visit(context.variableDefinitions()) as VariableDefinitions;
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
            NewNode(document, context);
            context.definition().Apply(childContext =>
            {
                var definition = Visit(childContext) as IDefinition;
                document.AddDefinition(definition);

                if (definition is FragmentDefinition)
                {
                    document.Fragments.Add(definition as FragmentDefinition);
                }
                else if (definition is Operation)
                {
                    document.Operations.Add(definition as Operation);
                }
                else
                {
                    throw new ExecutionError("Unhandled document definition");
                }
            });

            return document;
        }

        private void NewNode(AbstractNode node, ParserRuleContext context)
        {
            node.SourceLocation = GetSourceLocation(context);
        }

        private SourceLocation GetSourceLocation(ParserRuleContext context)
        {
            return new SourceLocation(context.Start.Line, context.Start.Column + 1);
        }
    }
}
