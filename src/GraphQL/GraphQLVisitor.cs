using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Parsing;

namespace GraphQL
{
    public class Document
    {
        public Document()
        {
            Operations = new Operations();
            Fragments = new Fragments();
        }

        public Operations Operations { get; set; }

        public Fragments Fragments { get; set; }
    }

    public class Operations : IEnumerable<Operation>
    {
        private readonly List<Operation> _operations = new List<Operation>();

        public void Add(Operation operation)
        {
            _operations.Add(operation);
        }

        public Operation WithName(string operationName)
        {
            return _operations.FirstOrDefault(op => op.Name == operationName);
        }

        public IEnumerator<Operation> GetEnumerator()
        {
            return _operations.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class Operation
    {
        public Operation()
        {
            OperationType = OperationType.Query;
            Directives = new Directives();
            Variables = new Variables();
        }

        public string Name { get; set; }

        public OperationType OperationType { get; set; }

        public Directives Directives { get; set; }

        public Variables Variables { get; set; }

        public Selections Selections { get; set; }
    }

    public class Selection
    {
        public Field Field { get; set; }

        public IFragment Fragment { get; set; }
    }

    public class Selections : IEnumerable<Selection>
    {
        private readonly List<Selection> _selections = new List<Selection>();

        public void Add(Selection selection)
        {
            _selections.Add(selection);
        }

        public IEnumerator<Selection> GetEnumerator()
        {
            return _selections.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class Variables : IEnumerable<Variable>
    {
        private readonly List<Variable> _variables = new List<Variable>();

        public void Add(Variable variable)
        {
            _variables.Add(variable);
        }

        public IEnumerator<Variable> GetEnumerator()
        {
            return _variables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class Variable
    {
        public string Name { get; set; }

        public VariableType Type { get; set; }

        public object DefaultValue { get; set; }

        public object Value { get; set; }
    }

    public class Fragments : IEnumerable<IFragment>
    {
        private readonly List<IFragment> _fragments = new List<IFragment>();

        public void Add(IFragment fragment)
        {
            _fragments.Add(fragment);
        }

        public IEnumerator<IFragment> GetEnumerator()
        {
            return _fragments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public interface IFragment
    {
    }

    public class FragmentSpread : IFragment
    {
        public FragmentSpread()
        {
            Directives = new Directives();
        }

        public string Name { get; set; }

        public Directives Directives { get; set; }
    }

    public class InlineFragment : IFragment
    {
        public string Type { get; set; }

        public Directives Directives { get; set; }

        public Selections Selections { get; set; }
    }

    public class FragmentDefinition : IFragment
    {
        public FragmentDefinition()
        {
            Directives = new Directives();
        }

        public string Name { get; set; }

        public string Type { get; set; }

        public Directives Directives { get; set; }

        public Selections Selections { get; set; }
    }

    public enum OperationType
    {
        Query,
        Mutation
    }

    public class Fields : IEnumerable<Field>
    {
        private readonly List<Field> _fields = new List<Field>();

        public void Add(Field field)
        {
            _fields.Add(field);
        }

        public IEnumerator<Field> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class Field
    {
        public string Name { get; set; }

        public string Alias { get; set; }

        public Directives Directives { get; set; }

        public Arguments Arguments { get; set; }

        public Selections Selections { get; set; }
    }

    public class Arguments : IEnumerable<Argument>
    {
        private readonly List<Argument> _arguments = new List<Argument>();

        public void Add(Argument arg)
        {
            _arguments.Add(arg);
        }

        public IEnumerator<Argument> GetEnumerator()
        {
            return _arguments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class Directives : IEnumerable<Directive>
    {
        private readonly List<Directive> _directives = new List<Directive>();

        public void Add(Directive directive)
        {
            _directives.Add(directive);
        }

        public IEnumerator<Directive> GetEnumerator()
        {
            return _directives.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class Directive
    {
        public string Name { get; set; }

        public object Value { get; set; }

        public Variable Variable { get; set; }
    }

    public class Argument
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public class VariableType
    {
        public VariableType()
        {
            AllowsNull = true;
        }

        public string Name { get; set; }

        public bool IsList { get; set; }

        public bool AllowsNull { get; set; }
    }

    public class GraphQLVisitor : GraphQLBaseVisitor<object>
    {
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

        public override object VisitStringValue(GraphQLParser.StringValueContext context)
        {
            return context.STRING().GetText();
        }

        public override object VisitNumberValue(GraphQLParser.NumberValueContext context)
        {
            return context.NUMBER().GetText();
        }

        public override object VisitBooleanValue(GraphQLParser.BooleanValueContext context)
        {
            return context.BOOLEAN().GetText();
        }

        public override object VisitArrayValue(GraphQLParser.ArrayValueContext context)
        {
            return context.array().GetText();
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

        public override object VisitArgument(GraphQLParser.ArgumentContext context)
        {
            var name = context.NAME().GetText();
            var valOrVar = context.valueOrVariable();
            var val = valOrVar.value() != null ? valOrVar.value().GetText() : valOrVar.variable().GetText();
            var arg = new Argument
            {
                Name = name,
                Value = val
            };

            Console.WriteLine("argument: {0}:{1}", arg.Name, arg.Value);

            return base.VisitArgument(context);
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
            Console.Write("directive: {0}", directive.Name);
            if (context.valueOrVariable() != null)
            {
                var val = "";
                var valOrVar = context.valueOrVariable();
                if (valOrVar.value() != null)
                {
                    directive.Value = valOrVar.value().GetText();
                    val = directive.Value.ToString();
                }
                else if (valOrVar.variable() != null)
                {
                    var name = valOrVar.variable().GetText().Substring(1);
                    directive.Variable = new Variable {Name = name};
                    val = name;
                }
                Console.Write(":{0}", val);
            }
            Console.WriteLine();

            return directive;
        }

        public override object VisitFragmentSpread(GraphQLParser.FragmentSpreadContext context)
        {
            var fragment = new FragmentSpread();
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
