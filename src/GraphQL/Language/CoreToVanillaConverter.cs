using System.Collections.Generic;
using System.Linq;
using GraphQL.Language.AST;
using GraphQLParser;
using GraphQLParser.AST;
using OperationTypeParser = GraphQLParser.AST.OperationType;
using OperationType = GraphQL.Language.AST.OperationType;

namespace GraphQL.Language
{
    public class CoreToVanillaConverter
    {
        private ISource _body;

        public Document Convert(string body, GraphQLDocument source)
        {
            _body = new Source(body);

            var target = new Document();

            AddDefinitions(source, target);

            return target;
        }

        public void AddDefinitions(GraphQLDocument source, Document target)
        {
            source.Definitions.Apply(def =>
            {
                var op = def as GraphQLOperationDefinition;
                if (op != null)
                {
                    target.AddDefinition(Operation(op));
                }

                var frag = def as GraphQLFragmentDefinition;
                if (frag != null)
                {
                    target.AddDefinition(Fragment(frag));
                }
            });
        }

        public Operation Operation(GraphQLOperationDefinition source)
        {
            var name = source.Name != null ? Name(source.Name) : null;
            var op = new Operation(name).WithLocation(source, _body);
            op.OperationType = ToOperationType(source.Operation);
            op.SelectionSet = SelectionSet(source.SelectionSet);
            op.Variables = VariableDefinitions(source.VariableDefinitions);
            op.Directives = Directives(source.Directives);
            return op;
        }

        public FragmentDefinition Fragment(GraphQLFragmentDefinition source)
        {
            var frag = new FragmentDefinition(Name(source.Name)).WithLocation(source, _body);
            frag.Type = NamedType(source.TypeCondition);
            frag.SelectionSet = SelectionSet(source.SelectionSet);
            frag.Directives = Directives(source.Directives);
            return frag;
        }

        public FragmentSpread FragmentSpread(GraphQLFragmentSpread source)
        {
            var name = source.Name != null ? Name(source.Name) : null;
            var spread = new FragmentSpread(name).WithLocation(source, _body);
            spread.Directives = Directives(source.Directives);
            return spread;
        }

        public InlineFragment InlineFragment(GraphQLInlineFragment source)
        {
            var frag = new InlineFragment().WithLocation(source, _body);
            frag.Type = source.TypeCondition != null ? NamedType(source.TypeCondition) : null;
            frag.Directives = Directives(source.Directives);
            frag.SelectionSet = SelectionSet(source.SelectionSet);
            return frag;
        }

        public VariableDefinitions VariableDefinitions(IEnumerable<GraphQLVariableDefinition> source)
        {
            var defs = new VariableDefinitions();
            var list = source?.Select(VariableDefinition);
            list?.Apply(defs.Add);
            return defs;
        }

        public VariableDefinition VariableDefinition(GraphQLVariableDefinition source)
        {
            var def = new VariableDefinition(Name(source.Variable.Name)).WithLocation(source, _body);
            def.Type = Type(source.Type);
            var val = source.DefaultValue as GraphQLValue;
            if (val != null)
            {
                def.DefaultValue = Value(val);
            }
            else if (source.DefaultValue != null && !(source.DefaultValue is GraphQLValue))
            {
                throw new ExecutionError($"Unknown default value: {source.DefaultValue}");
            }
            return def;
        }

        public SelectionSet SelectionSet(GraphQLSelectionSet source)
        {
            var set = new SelectionSet().WithLocation(source, _body);
            source?.Selections.Apply(s =>
            {
                set.Add(Selection(s));
            });
            return set;
        }

        public ISelection Selection(ASTNode source)
        {
            switch (source.Kind)
            {
                case ASTNodeKind.Field:
                {
                    return Field((GraphQLFieldSelection) source);
                }
                case ASTNodeKind.FragmentSpread:
                {
                    return FragmentSpread((GraphQLFragmentSpread) source);
                }
                case ASTNodeKind.InlineFragment:
                {
                    return InlineFragment((GraphQLInlineFragment) source);
                }
            }

            throw new ExecutionError($"Unmapped selection {source.Kind}");
        }

        public Field Field(GraphQLFieldSelection source)
        {
            var alias = source.Alias != null ? Name(source.Alias) : null;
            var field = new Field(alias, Name(source.Name)).WithLocation(source, _body);
            field.Arguments = Arguments(source.Arguments);
            field.Directives = Directives(source.Directives);
            field.SelectionSet = SelectionSet(source.SelectionSet);
            return field;
        }

        public Directives Directives(IEnumerable<GraphQLDirective> directives)
        {
            var target = new Directives();
            directives?.Apply(d =>
            {
                var dir = new Directive(Name(d.Name)).WithLocation(d, _body);
                dir.Arguments = Arguments(d.Arguments);
                target.Add(dir);
            });
            return target;
        }

        public Arguments Arguments(IEnumerable<GraphQLArgument> source)
        {
            var target = new Arguments();

            source.Apply(s =>
            {
                var arg = new Argument(Name(s.Name)).WithLocation(s.Name, _body);
                arg.Value = Value(s.Value);
                target.Add(arg);
            });

            return target;
        }

        public IValue Value(GraphQLValue source)
        {
            switch (source.Kind)
            {
                case ASTNodeKind.StringValue:
                {
                    var str = source as GraphQLScalarValue;
                    return new StringValue($"{str.Value}").WithLocation(str, _body);
                }
                case ASTNodeKind.IntValue:
                {
                    var str = source as GraphQLScalarValue;

                    int intResult;
                    if (int.TryParse(str.Value, out intResult))
                    {
                        var val = new IntValue(intResult).WithLocation(str, _body);
                        return val;
                    }

                    // If the value doesn't fit in an integer, revert to using long...
                    long longResult;
                    if (long.TryParse(str.Value, out longResult))
                    {
                        var val = new LongValue(longResult).WithLocation(str, _body);
                        return val;
                    }

                    throw new ExecutionError($"Invalid number {str.Value}");
                }
                case ASTNodeKind.FloatValue:
                {
                    var str = source as GraphQLScalarValue;
                    return new FloatValue(double.Parse(str.Value)).WithLocation(str, _body);
                }
                case ASTNodeKind.BooleanValue:
                {
                    var str = source as GraphQLScalarValue;
                    return new BooleanValue(bool.Parse(str.Value)).WithLocation(str, _body);
                }
                case ASTNodeKind.EnumValue:
                {
                    var str = source as GraphQLScalarValue;
                    return new EnumValue(str.Value).WithLocation(str, _body);
                }
                case ASTNodeKind.Variable:
                {
                    var vari = source as GraphQLVariable;
                    return new VariableReference(Name(vari.Name)).WithLocation(vari, _body);
                }
                case ASTNodeKind.ObjectValue:
                {
                    var obj = source as GraphQLObjectValue;
                    var fields = obj.Fields.Select(ObjectField);
                    return new ObjectValue(fields).WithLocation(obj, _body);
                }
                case ASTNodeKind.ListValue:
                {
                    var list = source as GraphQLListValue;
                    var values = list.Values.Select(Value);
                    return new ListValue(values).WithLocation(list, _body);
                }
            }

            throw new ExecutionError($"Unmapped value type {source.Kind}");
        }

        public ObjectField ObjectField(GraphQLObjectField source)
        {
            var field = new ObjectField(Name(source.Name), Value(source.Value)).WithLocation(source, _body);
            return field;
        }

        public NamedType NamedType(GraphQLNamedType source)
        {
            var type = new NamedType(Name(source.Name)).WithLocation(source, _body);
            return type;
        }

        public IType Type(GraphQLType type)
        {
            switch (type.Kind)
            {
                case ASTNodeKind.NamedType:
                {
                    var name = (GraphQLNamedType) type;
                    return new NamedType(Name(name.Name)).WithLocation(name, _body);
                }

                case ASTNodeKind.NonNullType:
                {
                    var nonNull = (GraphQLNonNullType) type;
                    return new NonNullType(Type(nonNull.Type)).WithLocation(nonNull, _body);
                }

                case ASTNodeKind.ListType:
                {
                    var list = (GraphQLListType) type;
                    return new ListType(Type(list.Type)).WithLocation(list, _body);
                }
            }

            throw new ExecutionError($"Unmapped type {type.Kind}");
        }

        public NameNode Name(GraphQLName name)
        {
            return new NameNode(name.Value).WithLocation(name, _body);
        }

        public static OperationType ToOperationType(OperationTypeParser type)
        {
            switch (type)
            {
                case OperationTypeParser.Query:
                    return OperationType.Query;

                case OperationTypeParser.Mutation:
                    return OperationType.Mutation;

                case OperationTypeParser.Subscription:
                    return OperationType.Subscription;
            }

            throw new ExecutionError($"Unmapped operation type {type}");
        }
    }

    public static class AstNodeExtensions
    {
        public static T WithLocation<T>(this T node, ASTNode astNode, ISource source)
            where T : AbstractNode
        {
            return node.WithLocation(0, 0, astNode?.Location.Start ?? -1, astNode?.Location.End ?? -1);
        }
    }
}
