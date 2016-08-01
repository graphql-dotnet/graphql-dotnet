using System;
using System.Linq;
using System.Text.RegularExpressions;
using GraphQL.Language.AST;

namespace GraphQL.Language
{
    public static class GraphQLParser2
    {
        public static Parser<Document> Document =>
            Definition.Many().Brackets((pos, defs) =>
            {
                var document = new Document().WithLocation(pos);
                defs.Value.Apply(def =>
                {
                    document.AddDefinition(def);

                    if (def is FragmentDefinition)
                    {
                        document.Fragments.Add((FragmentDefinition) def);
                    }

                    if (def is Operation)
                    {
                        document.Operations.Add((Operation) def);
                    }

                    throw new ExecutionError($"Unhandled document definition {def}.");
                });
                return document;
            });

        public static Parser<IDefinition> Definition => OperationDefinition.Or<IDefinition>(FragmentDefinition);

        public static Parser<Operation> OperationDefinition =>
            SelectionSet.Return(s =>
            {
                var operation = new Operation().WithLocation(s.Position);
                operation.SelectionSet = s.Value;
                return operation;
            }).Or(OperationType.Then(Name.Optional().Token(), VariableDefinitions.Optional().Token(), Directives.Optional().Token(), SelectionSet.Token(),
                (type, name, variables, directives, selection) =>
                {
                    var operation = new Operation(name.Value.GetOrDefault()).WithLocation(type.Position);
                    operation.Variables = variables.Value.GetOrElse(new VariableDefinitions());
                    operation.Directives = directives.Value.GetOrElse(new Directives());
                    operation.SelectionSet = selection.Value;
                    return operation;
                }));

        public static Parser<OperationType> OperationType =>
            Parse.Regex(new Regex("(query|mutation|subscription)", RegexOptions.IgnoreCase))
                .Return(r =>
                {
                    GraphQL.Language.OperationType type;
                    Enum.TryParse(r.Value, true, out type);
                    return type;
                });

        public static Parser<SelectionSet> SelectionSet => Selection.Many().Brackets((pos, selection) =>
        {
            var set = new SelectionSet().WithLocation(pos);
            selection.Value.Apply(set.Add);
            return set;
        });

        public static Parser<ISelection> Selection => Field.Or<ISelection>(InlineFragment).Or(FragmentSpread);

        public static Parser<Field> Field =>
            Alias.Optional().Then(Name.Token(), Arguments.Optional().Token(), Directives.Optional().Token(), SelectionSet.Optional().Token(),
                (alias, name, args, directives, selection) =>
                {
                    var field = new Field(alias.Value.GetOrDefault(), name.Value)
                        .WithLocation(alias.Value.IsDefined ? alias.Position : name.Position);

                    field.Arguments = args.Value.GetOrDefault();
                    field.Directives = directives.Value.GetOrDefault();
                    field.SelectionSet = selection.Value.GetOrDefault();

                    return field;
                });

        public static Parser<NameNode> Alias => Name.Token().Return(name => name.Value);

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
                var argument = new Argument(first.Value).WithLocation(first.Position);
                argument.Value = rest.Value;
                return argument;
            });

        public static Parser<FragmentSpread> FragmentSpread =>
            Parse.Regex("(...)").Then(FragmentName.Token(), Directives.Optional().Token(), (dots, name, directives) =>
            {
                var spread = new FragmentSpread(name.Value).WithLocation(dots.Position);
                if (directives.Value.IsDefined)
                {
                    spread.Directives = directives.Value.Get();
                }
                return spread;
            });

        public static Parser<InlineFragment> InlineFragment =>
            Parse.Regex("...")
                .Then(TypeCondition.Optional().Token(), Directives.Optional().Token(), SelectionSet.Token(),
                    (dots, type, directives, selection) =>
                    {
                        var frag = new InlineFragment().WithLocation(dots.Position);
                        frag.Type = type.Value.GetOrDefault();
                        frag.Directives = directives.Value.GetOrDefault();
                        frag.SelectionSet = selection.Value;
                        return frag;
                    });

        public static Parser<FragmentDefinition> FragmentDefinition =>
            Parse.Regex("(fragment)")
                .Then(FragmentName.Token(), TypeCondition.Token(), Directives.Optional().Token(), SelectionSet.Token(),
                    (frag, name, type, directives, selection) =>
                    {
                        var def = new FragmentDefinition(name.Value).WithLocation(frag.Position);
                        def.Type = type.Value;
                        def.Directives = directives.Value.GetOrDefault();
                        def.SelectionSet = selection.Value;
                        return def;
                    });

        public static Parser<NameNode> FragmentName => Name.Token().Return(name => name.Value);

        public static Parser<NamedType> TypeCondition =>
            Parse.Regex("(on)").Token().Then(NamedType, (on, type) => type.Value);

        public static Parser<ListValue> ListValue =>
            Parse.EmptyBrackets(pos =>
            {
                var list = new ListValue(Enumerable.Empty<IValue>()).WithLocation(pos);
                return list;
            })
            .Or(Value.Many().Brackets((pos, values) =>
            {
                var list = new ListValue(values.Value).WithLocation(pos);
                return list;
            }));

        public static Parser<ObjectValue> ObjectValue =>
            Parse.EmptyBraces(pos =>
            {
                var value = new ObjectValue(Enumerable.Empty<ObjectField>()).WithLocation(pos);
                return value;
            })
            .Or(ObjectField.Many().Braces((pos, fields) =>
            {
                var value = new ObjectValue(fields.Value).WithLocation(pos);
                return value;
            }));

        public static Parser<ObjectField> ObjectField =>
            Name.Then(Parse.Colon.Token(), Value, (name, colon, value) =>
            {
                var objField = new ObjectField(name.Value, value.Value).WithLocation(name.Position);
                return objField;
            });

        public static Parser<ListValue> ListValueWithVariable =>
            Parse.EmptyBrackets(pos =>
            {
                var list = new ListValue(Enumerable.Empty<IValue>()).WithLocation(pos);
                return list;
            })
            .Or(ValueWithVariable.Many().Brackets((pos, values) =>
            {
                var list = new ListValue(values.Value).WithLocation(pos);
                return list;
            }));

        public static Parser<ObjectValue> ObjectValueWithVariable =>
            Parse.EmptyBraces(pos =>
            {
                var value = new ObjectValue(Enumerable.Empty<ObjectField>()).WithLocation(pos);
                return value;
            })
            .Or(ObjectFieldWithVariable.Many().Braces((pos, fields) =>
            {
                var value = new ObjectValue(fields.Value).WithLocation(pos);
                return value;
            }));


        public static Parser<ObjectField> ObjectFieldWithVariable =>
            Name.Then(Parse.Colon.Token(), ValueWithVariable, (name, colon, value) =>
            {
                var objField = new ObjectField(name.Value, value.Value).WithLocation(name.Position);
                return objField;
            });

        public static Parser<IType> Type => NamedType
            .Or<IType>(ListType)
            .Or(NonNullType);

        public static Parser<IValue> DefaultValue => Parse.Eq.Once().Then(Value.Token(), (eq, value) => value.Value);

        public static Parser<ListType> ListType =>
                Type.Brackets((pos, type) =>
                {
                    var listType = new ListType(type.Value).WithLocation(pos);
                    return listType;
                });

        public static Parser<NonNullType> NonNullType =>
            NamedType.Then(Parse.Bang, (name, bang) => new NonNullType(name.Value).WithLocation(name.Position))
            .Or(ListType.Then(Parse.Bang, (list, bang) => new NonNullType(list.Value).WithLocation(list.Position)));

        public static Parser<NamedType> NamedType =>
            Name.Return(n => new NamedType(n.Value).WithLocation(n.Position));

        public static Parser<NameNode> Name
        {
            get
            {
                var allowedStart = Parse.CharRegex("[_A-Za-z]", "[_A-Za-z]").Once();
                var allowedNext = Parse.CharRegex("[_0-9A-Za-z]", "[_0-9A-Za-z]").Many();
                return allowedStart.Then(allowedNext, (first, rest) =>
                    new NameNode(first.Value.Concat(rest.Value).ToStr()).WithLocation(first.Position));
            }
        }

        public static Parser<EnumValue> EnumValue =>
            Name.Token().Return(name =>
            {
                var value = new EnumValue(name.Value).WithLocation(name.Position);
                return value;
            });

        public static Parser<IValue> ValueWithVariable => Variable
            .Or<IValue>(BooleanValue)
            .Or(FloatValue)
            .Or(IntValue)
            .Or(EnumValue);
//            .Or(ListValueWithVariable)
//            .Or(ObjectValueWithVariable);

        public static Parser<IValue> Value => BooleanValue
            .Or<IValue>(FloatValue)
            .Or(IntValue)
            .Or(EnumValue);
//            .Or(ListValue)
//            .Or(ObjectValue);

        public static Parser<VariableDefinitions> VariableDefinitions =>
            VariableDefinition.Many().Parens((pos, defs) =>
            {
                var definitions = new VariableDefinitions();
                defs.Value.Apply(definitions.Add);
                return definitions;
            });

        public static Parser<VariableDefinition> VariableDefinition =>
            Variable.Then(Parse.Colon, Type, (variable, colon, type) =>
            {
                var def = new VariableDefinition().WithLocation(variable.Position);
                def.Name = variable.Value.Name;
                def.Type = type.Value;
                return def;
            });

        public static Parser<VariableReference> Variable =>
            Parse.Dollar.Once().Then(Name, (first, rest) => new VariableReference(rest.Value).WithLocation(first.Position));

        public static Parser<IValue> IntValue => Parse.IntOrLong.Return<object, IValue>(f =>
        {
            if (f.Value is int)
            {
                return new IntValue((int)f.Value).WithLocation(f.Position.Line, f.Position.Column);
            }

            if (f.Value is long)
            {
                return new LongValue((long)f.Value).WithLocation(f.Position.Line, f.Position.Column);
            }

            throw new ArgumentOutOfRangeException($"Unsupported IntValue {f.Value}.");
        });

        public static Parser<FloatValue> FloatValue =>
            Parse.Double.Return(f => new FloatValue(f.Value).WithLocation(f.Position.Line, f.Position.Column));

        public static Parser<BooleanValue> BooleanValue =>
            Parse.Bool.Return(f => new BooleanValue(f.Value).WithLocation(f.Position.Line, f.Position.Column));

        public static Parser<Directives> Directives =>
            Directive.Many().Return(v =>
            {
                var directives = new Directives().WithLocation(v.Position);
                v.Value.Apply(directives.Add);
                return directives;
            });

        public static Parser<Directive> Directive =>
            Parse.At.Once().Then(Name, Arguments.Optional(), (first, name, args) =>
            {
                var directive = new Directive
                {
                    Name = name.Value.Name
                }.WithLocation(first.Position);

                directive.Arguments = args.Value.GetOrElse(new Arguments());

                return directive;
            });
    }
}
