using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GraphQL.Language.AST;
using GraphQLParser.AST;
using OperationType = GraphQL.Language.AST.OperationType;
using OperationTypeParser = GraphQLParser.AST.OperationType;

namespace GraphQL.Language
{
    /// <summary>
    /// Converts an GraphQLParser AST representation of a document into a GraphQL.NET AST
    /// representation of a document. Works only with executable definitions - operations
    /// and fragments, all other definitions are ignored.
    /// <br/>
    /// For more information see https://spec.graphql.org/June2018/#sec-Language.Document.
    /// </summary>
    public static class CoreToVanillaConverter
    {
        /// <summary>
        /// Converts an GraphQLParser AST representation of a document into a GraphQL.NET AST
        /// representation of a document and returns it.
        /// </summary>
        public static Document Convert(GraphQLDocument source)
        {
            var target = new Document();
            AddDefinitions(source, target);
            return target;
        }

        /// <summary>
        /// Enumerates the operations and fragments in the source document and adds them to the target document.
        /// </summary>
        private static void AddDefinitions(GraphQLDocument source, Document target)
        {
            if (source.Definitions != null)
            {
                foreach (var def in source.Definitions)
                {
                    if (def is GraphQLOperationDefinition op)
                    {
                        target.Operations.Add(Operation(op));
                    }
                    else if (def is GraphQLFragmentDefinition frag)
                    {
                        target.Fragments.Add(Fragment(frag));
                    }
                }
            }
        }

        /// <summary>
        /// Converts an operation node and its children.
        /// </summary>
        private static Operation Operation(GraphQLOperationDefinition source)
        {
            return new Operation(Name(source.Name), SelectionSet(source.SelectionSet))
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
                OperationType = ToOperationType(source.Operation),
                Variables = VariableDefinitions(source.VariableDefinitions),
                Directives = Directives(source.Directives)
            };
        }

        /// <summary>
        /// Converts a fragment definition node and its children.
        /// </summary>
        private static FragmentDefinition Fragment(GraphQLFragmentDefinition source)
        {
            return new FragmentDefinition(Name(source.Name), NamedType(source.TypeCondition!), SelectionSet(source.SelectionSet))
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
                Directives = Directives(source.Directives)
            };
        }

        /// <summary>
        /// Converts a fragment spread node and its children.
        /// </summary>
        private static FragmentSpread FragmentSpread(GraphQLFragmentSpread source)
        {
            return new FragmentSpread(Name(source.Name))
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
                Directives = Directives(source.Directives)
            };
        }

        /// <summary>
        /// Converts an inline fragment node and its children.
        /// </summary>
        private static InlineFragment InlineFragment(GraphQLInlineFragment source)
        {
            return new InlineFragment(SelectionSet(source.SelectionSet))
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
                Type = source.TypeCondition != null ? NamedType(source.TypeCondition) : null,
                Directives = Directives(source.Directives),
            };
        }

        /// <summary>
        /// Converts a list of variable definition nodes and their children.
        /// </summary>
        private static VariableDefinitions? VariableDefinitions(List<GraphQLVariableDefinition>? source)
        {
            VariableDefinitions? defs = null;

            if (source?.Count > 0)
            {
                defs = new VariableDefinitions(source.Count);
                foreach (var def in source)
                    defs.Add(VariableDefinition(def));
            }

            return defs;
        }

        /// <summary>
        /// Converts a variable definition node and its children.
        /// </summary>
        private static VariableDefinition VariableDefinition(GraphQLVariableDefinition source)
        {
            var def = new VariableDefinition(Name(source.Variable!.Name), Type(source.Type!))
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
            };
            if (source.DefaultValue is GraphQLValue val)
            {
                def.DefaultValue = Value(val);
            }
            else if (source.DefaultValue != null && source.DefaultValue is not GraphQLValue)
            {
                throw new InvalidOperationException($"Unknown default value: {source.DefaultValue}");
            }
            return def;
        }

        /// <summary>
        /// Converts a selection set node and its children.
        /// </summary>
        private static SelectionSet SelectionSet(GraphQLSelectionSet? source)
        {
            var set = new SelectionSet();

            if (source != null)
            {
                set.SourceLocation = Convert(source.Location);
                if (source.Selections != null)
                {
                    foreach (var s in source.Selections)
                    {
                        set.Add(Selection(s));
                    }
                }
            }

            return set;
        }

        /// <summary>
        /// Converts a selection node and its children.
        /// </summary>
        private static ISelection Selection(ASTNode source) => source.Kind switch
        {
            ASTNodeKind.Field => Field((GraphQLFieldSelection)source),
            ASTNodeKind.FragmentSpread => FragmentSpread((GraphQLFragmentSpread)source),
            ASTNodeKind.InlineFragment => InlineFragment((GraphQLInlineFragment)source),
            _ => throw new InvalidOperationException($"Unmapped selection {source.Kind}")
        };

        /// <summary>
        /// Converts a field node and its children.
        /// </summary>
        private static Field Field(GraphQLFieldSelection source)
        {
            return new Field(Name(source.Alias), Name(source.Name))
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
                Arguments = Arguments(source.Arguments),
                Directives = Directives(source.Directives),
                SelectionSet = SelectionSet(source.SelectionSet)
            };
        }

        /// <summary>
        /// Converts a list of directive nodes and their children.
        /// </summary>
        private static Directives? Directives(List<GraphQLDirective>? source)
        {
            Directives? target = null;

            if (source?.Count > 0)
            {
                target = new Directives(source.Count);
                foreach (var d in source)
                    target.Add(Directive(d));
            }

            return target;
        }

        /// <summary>
        /// Converts a directive node and its children.
        /// </summary>
        internal static Directive Directive(GraphQLDirective d)
        {
            return new Directive(Name(d.Name))
            {
                SourceLocation = Convert(d.Location),
                Arguments = Arguments(d.Arguments)
            };
        }

        /// <summary>
        /// Converts a list of argument nodes and their children.
        /// </summary>
        private static Arguments? Arguments(List<GraphQLArgument>? source)
        {
            Arguments? target = null;

            if (source?.Count > 0)
            {
                target = new Arguments(source.Count);

                foreach (var a in source)
                {
                    var arg = new Argument(Name(a.Name), Value(a.Value!))
                    {
                        SourceLocation = Convert(a.Name!.Location),
                        CommentNode = Comment(a.Comment),
                    };
                    target.Add(arg);
                }
            }

            return target;
        }

        /// <summary>
        /// Converts a value node and its children.
        /// </summary>
        internal static IValue Value(GraphQLValue source)
        {
            switch (source.Kind)
            {
                case ASTNodeKind.StringValue:
                {
                    var str = (GraphQLScalarValue)source;
                    return new StringValue((string)str.Value) { SourceLocation = Convert(str.Location) };
                }
                case ASTNodeKind.IntValue:
                {
                    var str = (GraphQLScalarValue)source;

                    if (Int.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int intResult))
                    {
                        return new IntValue(intResult) { SourceLocation = Convert(str.Location) };
                    }

                    // If the value doesn't fit in an integer, revert to using long...
                    if (Long.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long longResult))
                    {
                        return new LongValue(longResult) { SourceLocation = Convert(str.Location) };
                    }

                    // If the value doesn't fit in an long, revert to using decimal...
                    if (Decimal.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal decimalResult))
                    {
                        return new DecimalValue(decimalResult) { SourceLocation = Convert(str.Location) };
                    }

                    // If the value doesn't fit in an decimal, revert to using BigInteger...
                    if (BigInt.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var bigIntegerResult))
                    {
                        return new BigIntValue(bigIntegerResult) { SourceLocation = Convert(str.Location) };
                    }

                    // Since BigInteger can contain any valid integer (arbitrarily large), this is impossible to trigger via an invalid query
                    throw new InvalidOperationException($"Invalid number {str.Value}");
                }
                case ASTNodeKind.FloatValue:
                {
                    var str = (GraphQLScalarValue)source;

                    // the idea is to see if there is a loss of accuracy of value
                    // for example, 12.1 or 12.11 is double but 12.10 is decimal
                    if (!Double.TryParse(
                        str.Value,
                        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                        CultureInfo.InvariantCulture,
                        out double dbl))
                    {
                        dbl = str.Value.Span[0] == '-' ? double.NegativeInfinity : double.PositiveInfinity;
                    }

                    //it is possible for a FloatValue to overflow a decimal; however, with a double, it just returns Infinity or -Infinity
                    if (Decimal.TryParse(
                        str.Value,
                        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                        CultureInfo.InvariantCulture,
                        out decimal dec))
                    {
                        // Cast the decimal to our struct to avoid the decimal.GetBits allocations.
                        var decBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref dec);
                        decimal temp = new decimal(dbl);
                        var dblAsDecBits = System.Runtime.CompilerServices.Unsafe.As<decimal, DecimalData>(ref temp);
                        if (!decBits.Equals(dblAsDecBits))
                            return new DecimalValue(dec) { SourceLocation = Convert(str.Location) };
                    }

                    return new FloatValue(dbl) { SourceLocation = Convert(str.Location) };
                }
                case ASTNodeKind.BooleanValue:
                {
                    var str = (GraphQLScalarValue)source;
                    return new BooleanValue(str.Value.Length == 4 /*true.Length=4*/) { SourceLocation = Convert(str.Location) };
                }
                case ASTNodeKind.EnumValue:
                {
                    var str = (GraphQLScalarValue)source;
                    return new EnumValue((string)str.Value) { SourceLocation = Convert(str.Location) };
                }
                case ASTNodeKind.Variable:
                {
                    var vari = (GraphQLVariable)source;
                    return new VariableReference(Name(vari.Name)) { SourceLocation = Convert(vari.Location) };
                }
                case ASTNodeKind.ObjectValue:
                {
                    var obj = (GraphQLObjectValue)source;
                    var fields = obj.Fields?.Select(ObjectField) ?? Enumerable.Empty<ObjectField>();
                    return new ObjectValue(fields) { SourceLocation = Convert(obj.Location) };
                }
                case ASTNodeKind.ListValue:
                {
                    var list = (GraphQLListValue)source;
                    var values = list.Values?.Select(Value) ?? Enumerable.Empty<IValue>();
                    return new ListValue(values) { SourceLocation = Convert(list.Location) };
                }
                case ASTNodeKind.NullValue:
                {
                    var str = (GraphQLScalarValue)source;
                    return new NullValue { SourceLocation = Convert(str.Location) };
                }
            }

            throw new InvalidOperationException($"Unmapped value type {source.Kind}");
        }

        /// <summary>
        /// Converts and object field node and its children.
        /// </summary>
        private static ObjectField ObjectField(GraphQLObjectField source)
        {
            return new ObjectField(Name(source.Name), Value(source.Value!)) { SourceLocation = Convert(source.Location) };
        }

        /// <summary>
        /// Converts a named type node and its children.
        /// </summary>
        private static NamedType NamedType(GraphQLNamedType source)
        {
            return new NamedType(Name(source.Name)) { SourceLocation = Convert(source.Location) };
        }

        /// <summary>
        /// Converts a type node and its children.
        /// </summary>
        private static IType Type(GraphQLType type)
        {
            switch (type.Kind)
            {
                case ASTNodeKind.NamedType:
                {
                    var name = (GraphQLNamedType)type;
                    return new NamedType(Name(name.Name)) { SourceLocation = Convert(name.Location) };
                }

                case ASTNodeKind.NonNullType:
                {
                    var nonNull = (GraphQLNonNullType)type;
                    return new NonNullType(Type(nonNull.Type!)) { SourceLocation = Convert(nonNull.Location) };
                }

                case ASTNodeKind.ListType:
                {
                    var list = (GraphQLListType)type;
                    return new ListType(Type(list.Type!)) { SourceLocation = Convert(list.Location) };
                }
            }

            throw new InvalidOperationException($"Unmapped type {type.Kind}");
        }

        /// <summary>
        /// Converts a name node.
        /// </summary>
        private static NameNode Name(GraphQLName? name)
        {
            return name == null
                ? default
                : new NameNode((string)name.Value, Convert(name.Location));
        }

        /// <summary>
        /// Converts a comment node.
        /// </summary>
        private static CommentNode? Comment(GraphQLComment? comment)
        {
            return comment == null
                ? null
                : new CommentNode((string)comment.Text) { SourceLocation = Convert(comment.Location) };
        }

        /// <summary>
        /// Converts an operation type enumeration value.
        /// </summary>
        private static OperationType ToOperationType(OperationTypeParser type) => type switch
        {
            OperationTypeParser.Query => OperationType.Query,
            OperationTypeParser.Mutation => OperationType.Mutation,
            OperationTypeParser.Subscription => OperationType.Subscription,
            _ => throw new InvalidOperationException($"Unmapped operation type {type}")
        };

        /// <summary>
        /// Converts a location reference within a document.
        /// </summary>
        private static SourceLocation Convert(GraphQLLocation location) => new SourceLocation(location.Start, location.End);
    }
}
