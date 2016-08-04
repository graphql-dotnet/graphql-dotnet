﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using GraphQL.Language.AST;

namespace GraphQL.Language
{
    public static class GraphQLParser2
    {
        public static Parser<Document> Document =>
            Definition.Many().Return(defs =>
            {
                var document = new Document();
                var first = defs.Value.FirstOrDefault();
                if (first != null)
                {
                    document.WithLocation(first.SourceLocation.Line, first.SourceLocation.Column);
                }
                defs.Value.Apply(def =>
                {
                    document.AddDefinition(def);

                    var fragment = def as FragmentDefinition;
                    if (fragment != null)
                    {
                        document.Fragments.Add(fragment);
                        return;
                    }

                    var operation = def as Operation;
                    if (operation != null)
                    {
                        document.Operations.Add(operation);
                        return;
                    }

                    throw new ExecutionError($"Unhandled document definition {def}.");
                });
                return document;
            });

        public static Parser<IDefinition> Definition => OperationDefinition.Token().Or<IDefinition>(FragmentDefinition.Token());

        public static Parser<Operation> OperationDefinition =>
            OperationType.Token().Then(
                Parse.Ref(() => Name.Optional().Token()),
                VariableDefinitions.Optional().Token(),
                Directives.Optional().Token(),
                Parse.Ref(() => SelectionSet.Token()),
                (type, name, variables, directives, selection) =>
                {
                    var operation = new Operation(name.Value.GetOrDefault()).WithLocation(type.Position.Value.Line, type.Position.Value.Column);
                    operation.OperationType = type.Value;
                    operation.Variables = variables.Value.GetOrElse(new VariableDefinitions());
                    operation.Directives = directives.Value.GetOrElse(new Directives());
                    operation.SelectionSet = selection.Value;
                    return operation;
                }).Or(SelectionSet.Return(s =>
                {
                    var operation = new Operation().WithLocation(s.Position.Value.Line, s.Position.Value.Column);
                    operation.SelectionSet = s.Value;
                    return operation;
                }));

        private static readonly Regex operationTypeRegex = new Regex("(query|mutation|subscription)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Parser<OperationType> OperationType =>
            Parse.Regex(operationTypeRegex)
                .Return(r =>
                {
                    OperationType type;
                    Enum.TryParse(r.Value, true, out type);
                    return type;
                });

        public static Parser<SelectionSet> SelectionSet => Selection.Many().Braces((pos, selection) =>
        {
            var set = new SelectionSet().WithLocation(pos.Line, pos.Column);
            selection.Value.Apply(set.Add);
            return set;
        }).Named("selection set");

        public static Parser<ISelection> Selection => FieldWithAlias.Or<ISelection>(Field).Or(InlineFragment).Or(FragmentSpread);

        public static Parser<Field> Field =>
                Name.Token().Then(
                    Arguments.Optional(),
                    Directives.Optional(),
                    Parse.Ref(()=>SelectionSet).Optional(),
                    (name, args, directives, set)=>
                {
                    var field = new Field(null, name.Value)
                        .WithLocation(name.Position.Value);

                    field.Arguments = args.Value.GetOrDefault();
                    field.Directives = directives.Value.GetOrDefault();
                    field.SelectionSet = set.Value.GetOrDefault();

                    return field;
                }).Named("field");

        public static Parser<Field> FieldWithAlias =>
                Alias.Then(
                    Arguments.Optional(),
                    Directives.Optional(),
                    Parse.Ref(()=>SelectionSet).Optional(),
                    (alias, args, directives, set)=>
                {
                    var field = new Field(alias.Value.Al, alias.Value.Name)
                        .WithLocation(alias.Position.Value);

                    field.Arguments = args.Value.GetOrDefault();
                    field.Directives = directives.Value.GetOrDefault();
                    field.SelectionSet = set.Value.GetOrDefault();

                    return field;
                }).Named("field with alias");

        public static Parser<Alias> Alias =>
            Name.Token().Then(Parse.Colon.Token(), Name.Token(), (alias, colon, name) => new Alias(alias.Value, name.Value)).Named("alias");

        public static Parser<Arguments> Arguments =>
            Argument.Many().Parens((pos, args) =>
            {
                var arguments = new Arguments().WithLocation(pos);
                args.Value.Apply(arguments.Add);
                return arguments;
            });

        public static Parser<Argument> Argument =>
            Name.Then(Parse.Colon.Token(), ValueWithVariable.Token(), (first, middle, rest) =>
            {
                var argument = new Argument(first.Value).WithLocation(first.Position.Value);
                argument.Value = rest.Value;
                return argument;
            });

        private static readonly Regex fragmentSpreadRegex = new Regex("(\\.\\.\\.)", RegexOptions.Compiled);
        public static Parser<FragmentSpread> FragmentSpread =>
            Parse.Regex(fragmentSpreadRegex).Then(FragmentName.Token(), Directives.Optional().Token(), (dots, name, directives) =>
            {
                var spread = new FragmentSpread(name.Value).WithLocation(dots.Position.Value);
                if (directives.Value.IsDefined)
                {
                    spread.Directives = directives.Value.Get();
                }
                return spread;
            });

        private static readonly Regex inlineFragmentRegex = new Regex("\\.\\.\\.", RegexOptions.Compiled);
        public static Parser<InlineFragment> InlineFragment =>
            Parse.Regex(inlineFragmentRegex)
                .Then(TypeCondition.Optional().Token(), Directives.Optional().Token(), Parse.Ref(()=> SelectionSet).Token(),
                    (dots, type, directives, selection) =>
                    {
                        var frag = new InlineFragment().WithLocation(dots.Position.Value);
                        frag.Type = type.Value.GetOrDefault();
                        frag.Directives = directives.Value.GetOrDefault();
                        frag.SelectionSet = selection.Value;
                        return frag;
                    });

        private static readonly Regex fragmentDefinitionRegex = new Regex("(fragment)", RegexOptions.Compiled);
        public static Parser<FragmentDefinition> FragmentDefinition =>
            Parse.Regex(fragmentDefinitionRegex)
                .Then(FragmentName.Token(), TypeCondition.Token(), Directives.Optional().Token(), SelectionSet.Token(),
                    (frag, name, type, directives, selection) =>
                    {
                        var def = new FragmentDefinition(name.Value).WithLocation(frag.Position.Value);
                        def.Type = type.Value;
                        def.Directives = directives.Value.GetOrDefault();
                        def.SelectionSet = selection.Value;
                        return def;
                    });

        public static Parser<NameNode> FragmentName => Name.Token().Return(name => name.Value);

        private static readonly Regex typeConditionRegex = new Regex("(on)", RegexOptions.Compiled);
        public static Parser<NamedType> TypeCondition =>
            Parse.Regex(typeConditionRegex).Token().Then(NamedType, (on, type) => type.Value);

        public static Parser<ListValue> ListValue =>
            Parse.Ref(() => Value.Token().Many()).Brackets((pos, values) =>
            {
                var list = new ListValue(values.Value).WithLocation(pos.Line, pos.Column);
                return list;
            })
            .Or(Parse.EmptyBrackets(pos =>
            {
                var list = new ListValue(Enumerable.Empty<IValue>()).WithLocation(pos.Line, pos.Column);
                return list;
            }));

        public static Parser<ObjectValue> ObjectValue =>
            ObjectField.Many().Braces((pos, fields) =>
            {
                var value = new ObjectValue(fields.Value).WithLocation(pos.Line, pos.Column);
                return value;
            })
            .Or(Parse.EmptyBraces((pos, rest) =>
            {
                var value = new ObjectValue(Enumerable.Empty<ObjectField>()).WithLocation(pos.Line, pos.Column);
                return value;
            }));

        public static Parser<ObjectField> ObjectField =>
            Name.Then(Parse.Colon.Token(), Parse.Ref(() => Value.Token()), (name, colon, value) =>
            {
                var objField = new ObjectField(name.Value, value.Value).WithLocation(name.Position.Value);
                return objField;
            });

        public static Parser<ListValue> ListValueWithVariable =>
            Parse.EmptyBrackets(pos =>
            {
                var list = new ListValue(Enumerable.Empty<IValue>()).WithLocation(pos.Line, pos.Column);
                return list;
            })
            .Or(Parse.Ref(() => ValueWithVariable.Token().Many()).Brackets((pos, values) =>
            {
                var list = new ListValue(values.Value).WithLocation(pos.Line, pos.Column);
                return list;
            }));

        public static Parser<ObjectValue> ObjectValueWithVariable =>
            Parse.Ref(() => ObjectFieldWithVariable.Many()).Braces((pos, fields) =>
            {
                var value = new ObjectValue(fields.Value).WithLocation(pos.Line, pos.Column);
                return value;
            })
            .Or(Parse.EmptyBraces((pos, rest) =>
            {
                var value = new ObjectValue(Enumerable.Empty<ObjectField>()).WithLocation(pos.Line, pos.Column);
                return value;
            }));

        public static Parser<ObjectField> ObjectFieldWithVariable =>
            Name.Then(Parse.Colon.Token(), Parse.Ref(() => ValueWithVariable.Token()), (name, colon, value) =>
            {
                var objField = new ObjectField(name.Value, value.Value).WithLocation(name.Position.Value);
                return objField;
            });

        public static Parser<IType> Type =>
            Parse.Ref(() => NonNullType.Token())
                .Or<IType>(Parse.Ref(() => ListType.Token()))
                .Or(Parse.Ref(() => NamedType.Token()));

        public static Parser<IValue> DefaultValue => Parse.Eq.Once().Then(Value.Token(), (eq, value) => value.Value);

        public static Parser<ListType> ListType =>
                Type.Brackets((pos, type) =>
                {
                    var listType = new ListType(type.Value).WithLocation(pos);
                    return listType;
                });

        public static Parser<NonNullType> NonNullType => NonNullNamedType.Or(NonNullListType);

        public static Parser<NonNullType> NonNullListType =>
            ListType.Then(Parse.Bang.AtLeastOnce(), (name, bang) =>
            {
                var root = new NonNullType(name.Value).WithLocation(name.Position.Value);

                bang.Value.Skip(1).Apply(c =>
                {
                    root = new NonNullType(root).WithLocation(name.Position.Value);
                });

                return root;
            });

        public static Parser<NonNullType> NonNullNamedType =>
            NamedType.Then(Parse.Bang.AtLeastOnce(), (name, bang) =>
            {
                var root = new NonNullType(name.Value).WithLocation(name.Position.Value);

                bang.Value.Skip(1).Apply(c =>
                {
                    root = new NonNullType(root).WithLocation(name.Position.Value);
                });

                return root;
            });

        public static Parser<NamedType> NamedType =>
            Parse.Ref(() => Name).Return(n => new NamedType(n.Value).WithLocation(n.Position.Value));

        private static readonly Regex startOfNameRegex = new Regex("[_A-Za-z]", RegexOptions.Compiled);
        private static readonly Regex restOfNameRegex = new Regex("[_0-9A-Za-z]", RegexOptions.Compiled);
        public static Parser<NameNode> Name
        {
            get
            {
                var allowedStart = Parse.CharRegex(startOfNameRegex, "start of name [_A-Za-z]").Once();
                var allowedNext = Parse.CharRegex(restOfNameRegex, "rest of name [_0-9A-Za-z]").Many();
                return allowedStart.Then(allowedNext, (first, rest) =>
                    new NameNode(first.Value.Concat(rest.Value).ToStr()).WithLocation(first.Position.Value));
            }
        }

        public static Parser<EnumValue> EnumValue =>
            Parse.Ref(() => Name).Token().Return(name =>
            {
                var value = new EnumValue(name.Value).WithLocation(name.Position.Value);
                return value;
            });

        public static Parser<IValue> ValueWithVariable => Variable
            .Or<IValue>(FloatValue)
            .Or(IntValue)
            .Or(StringValue)
            .Or(BooleanValue)
            .Or(EnumValue)
            .Or(ListValueWithVariable)
            .Or(ObjectValueWithVariable);

        public static Parser<IValue> Value => FloatValue
            .Or(IntValue)
            .Or(StringValue)
            .Or(BooleanValue)
            .Or(EnumValue)
            .Or(ListValue)
            .Or(ObjectValue);

        public static Parser<VariableDefinitions> VariableDefinitions =>
            VariableDefinition.Many().Parens((pos, defs) =>
            {
                var definitions = new VariableDefinitions();
                defs.Value.Apply(definitions.Add);
                return definitions;
            });

        public static Parser<VariableDefinition> VariableDefinition =>
            Variable.Then(Parse.Colon.Token(), Type.Token(), DefaultValue.Optional().Token(), (variable, colon, type, defaultValue) =>
            {
                var def = new VariableDefinition().WithLocation(variable.Position.Value);
                def.Name = variable.Value.Name;
                def.Type = type.Value;
                def.DefaultValue = defaultValue.Value.GetOrDefault();
                return def;
            });

        public static Parser<VariableReference> Variable =>
            Parse.Dollar.Once().Then(Name, (first, rest) => new VariableReference(rest.Value).WithLocation(first.Position.Value));

        public static Parser<IValue> IntValue => Parse.IntOrLong.Return<object, IValue>(f =>
        {
            if (f.Value is int)
            {
                return new IntValue((int)f.Value).WithLocation(f.Position.Value.Line, f.Position.Value.Column);
            }

            if (f.Value is long)
            {
                return new LongValue((long)f.Value).WithLocation(f.Position.Value.Line, f.Position.Value.Column);
            }

            throw new ArgumentOutOfRangeException($"Unsupported IntValue {f.Value}.");
        });

        public static Parser<FloatValue> FloatValue =>
            Parse.Double.Return(f => new FloatValue(f.Value).WithLocation(f.Position.Value.Line, f.Position.Value.Column));

        public static Parser<StringValue> StringValue =>
            Parse.StringLiteral.Return(s => new StringValue(s.Value).WithLocation(s.Position.Value.Line, s.Position.Value.Column));

        public static Parser<BooleanValue> BooleanValue =>
            Parse.Bool.Return(f => new BooleanValue(f.Value).WithLocation(f.Position.Value.Line, f.Position.Value.Column));

        public static Parser<Directives> Directives =>
            Directive.Many().Return(v =>
            {
                var directives = new Directives().WithLocation(v.Position.Value);
                v.Value.Apply(directives.Add);
                return directives;
            });

        public static Parser<Directive> Directive =>
            Parse.At.Once().Then(Name, Arguments.Optional(), (first, name, args) =>
            {
                var directive = new Directive
                {
                    Name = name.Value.Name
                }.WithLocation(first.Position.Value);

                directive.Arguments = args.Value.GetOrElse(new Arguments());

                return directive;
            });
    }
}
