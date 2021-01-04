using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using GraphQL.Language.AST;
using GraphQLParser.AST;
using OperationType = GraphQL.Language.AST.OperationType;
using OperationTypeParser = GraphQLParser.AST.OperationType;

namespace GraphQL.Language
{
    public static class CoreToVanillaConverter
    {
        public static Document Convert(GraphQLDocument source)
        {
            var target = new Document();
            AddDefinitions(source, target);
            return target;
        }

        public static void AddDefinitions(GraphQLDocument source, Document target)
        {
            foreach (var def in source.Definitions)
            {
                if (def is GraphQLOperationDefinition op)
                {
                    target.AddDefinition(Operation(op));
                }

                if (def is GraphQLFragmentDefinition frag)
                {
                    target.AddDefinition(Fragment(frag));
                }
            }
        }

        public static Operation Operation(GraphQLOperationDefinition source)
        {
            return new Operation(source.Name?.Value)
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
                OperationType = ToOperationType(source.Operation),
                SelectionSet = SelectionSet(source.SelectionSet),
                Variables = VariableDefinitions(source.VariableDefinitions),
                Directives = Directives(source.Directives)
            };
        }

        public static FragmentDefinition Fragment(GraphQLFragmentDefinition source)
        {
            return new FragmentDefinition(source.Name?.Value)
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
                Type = NamedType(source.TypeCondition),
                SelectionSet = SelectionSet(source.SelectionSet),
                Directives = Directives(source.Directives)
            };
        }

        public static FragmentSpread FragmentSpread(GraphQLFragmentSpread source)
        {
            return new FragmentSpread(source.Name?.Value)
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
                Directives = Directives(source.Directives)
            };
        }

        public static InlineFragment InlineFragment(GraphQLInlineFragment source)
        {
            return new InlineFragment
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
                Type = source.TypeCondition != null ? NamedType(source.TypeCondition) : null,
                Directives = Directives(source.Directives),
                SelectionSet = SelectionSet(source.SelectionSet)
            };
        }

        public static VariableDefinitions VariableDefinitions(IEnumerable<GraphQLVariableDefinition> source)
        {
            VariableDefinitions defs = null;

            if (source != null)
            {
                foreach (var def in source.Select(VariableDefinition))
                {
                    (defs ??= new VariableDefinitions()).Add(def);
                }
            }

            return defs;
        }

        public static VariableDefinition VariableDefinition(GraphQLVariableDefinition source)
        {
            var def = new VariableDefinition(source.Variable.Name?.Value)
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
                Type = Type(source.Type)
            };
            if (source.DefaultValue is GraphQLValue val)
            {
                def.DefaultValue = Value(val);
            }
            else if (source.DefaultValue != null && !(source.DefaultValue is GraphQLValue))
            {
                throw new InvalidOperationException($"Unknown default value: {source.DefaultValue}");
            }
            return def;
        }

        public static SelectionSet SelectionSet(GraphQLSelectionSet source)
        {
            var set = new SelectionSet();

            if (source != null)
            {
                set.SourceLocation = Convert(source.Location);
                foreach (var s in source.Selections)
                {
                    set.Add(Selection(s));
                }
            }

            return set;
        }

        public static ISelection Selection(ASTNode source) => source.Kind switch
        {
            ASTNodeKind.Field => Field((GraphQLFieldSelection)source),
            ASTNodeKind.FragmentSpread => FragmentSpread((GraphQLFragmentSpread)source),
            ASTNodeKind.InlineFragment => InlineFragment((GraphQLInlineFragment)source),
            _ => throw new InvalidOperationException($"Unmapped selection {source.Kind}")
        };

        public static Field Field(GraphQLFieldSelection source)
        {
            return new Field(source.Alias?.Value, source.Name?.Value)
            {
                SourceLocation = Convert(source.Location),
                CommentNode = Comment(source.Comment),
                Arguments = Arguments(source.Arguments),
                Directives = Directives(source.Directives),
                SelectionSet = SelectionSet(source.SelectionSet)
            };
        }

        public static Directives Directives(IEnumerable<GraphQLDirective> source)
        {
            Directives target = null;

            if (source != null)
            {
                foreach (var d in source)
                {
                    (target ??= new Directives()).Add(Directive(d));
                }
            }

            return target;
        }

        public static Directive Directive(GraphQLDirective d)
        {
            return new Directive(d.Name?.Value)
            {
                SourceLocation = Convert(d.Location),
                Arguments = Arguments(d.Arguments)
            };
        }

        public static Arguments Arguments(IEnumerable<GraphQLArgument> source)
        {
            Arguments target = null;

            if (source != null)
            {
                foreach (var a in source)
                {
                    var arg = new Argument(a.Name?.Value)
                    {
                        SourceLocation = Convert(a.Name?.Location ?? a.Location),
                        CommentNode = Comment(a.Comment),
                        Value = Value(a.Value)
                    };
                    (target ??= new Arguments()).Add(arg);
                }
            }

            return target;
        }

        public static IValue Value(GraphQLValue source)
        {
            switch (source.Kind)
            {
                case ASTNodeKind.StringValue:
                {
                    var str = (GraphQLScalarValue)source;
                    return new StringValue(str.Value) { SourceLocation = Convert(str.Location) };
                }
                case ASTNodeKind.IntValue:
                {
                    var str = (GraphQLScalarValue)source;

                    if (int.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var intResult))
                    {
                        return new IntValue(intResult) { SourceLocation = Convert(str.Location) };
                    }

                    // If the value doesn't fit in an integer, revert to using long...
                    if (long.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var longResult))
                    {
                        return new LongValue(longResult) { SourceLocation = Convert(str.Location) };
                    }

                    // If the value doesn't fit in an long, revert to using decimal...
                    if (decimal.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var decimalResult))
                    {
                        return new DecimalValue(decimalResult) { SourceLocation = Convert(str.Location) };
                    }

                    // If the value doesn't fit in an decimal, revert to using BigInteger...
                    if (BigInteger.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var bigIntegerResult))
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
                    if (double.TryParse(
                        str.Value,
                        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                        CultureInfo.InvariantCulture,
                        out var dbl) == false)
                    {
                        dbl = str.Value[0] == '-' ? double.NegativeInfinity : double.PositiveInfinity;
                    }

                    //it is possible for a FloatValue to overflow a decimal; however, with a double, it just returns Infinity or -Infinity
                    if (decimal.TryParse(
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
                    return new BooleanValue(bool.Parse(str.Value)) { SourceLocation = Convert(str.Location) };
                }
                case ASTNodeKind.EnumValue:
                {
                    var str = (GraphQLScalarValue)source;
                    return new EnumValue(str.Value) { SourceLocation = Convert(str.Location) };
                }
                case ASTNodeKind.Variable:
                {
                    var vari = (GraphQLVariable)source;
                    return new VariableReference(vari.Name?.Value) { SourceLocation = Convert(vari.Location) };
                }
                case ASTNodeKind.ObjectValue:
                {
                    var obj = (GraphQLObjectValue)source;
                    var fields = obj.Fields?.Select(ObjectField);
                    return new ObjectValue(fields) { SourceLocation = Convert(obj.Location) };
                }
                case ASTNodeKind.ListValue:
                {
                    var list = (GraphQLListValue)source;
                    var values = list.Values?.Select(Value);
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

        public static ObjectField ObjectField(GraphQLObjectField source)
        {
            return new ObjectField(source.Name?.Value, Value(source.Value)) { SourceLocation = Convert(source.Location) };
        }

        public static NamedType NamedType(GraphQLNamedType source)
        {
            return new NamedType(source.Name?.Value) { SourceLocation = Convert(source.Location) };
        }

        public static IType Type(GraphQLType type)
        {
            switch (type.Kind)
            {
                case ASTNodeKind.NamedType:
                {
                    var name = (GraphQLNamedType)type;
                    return new NamedType(name.Name?.Value) { SourceLocation = Convert(name.Location) };
                }

                case ASTNodeKind.NonNullType:
                {
                    var nonNull = (GraphQLNonNullType)type;
                    return new NonNullType(Type(nonNull.Type)) { SourceLocation = Convert(nonNull.Location) };
                }

                case ASTNodeKind.ListType:
                {
                    var list = (GraphQLListType)type;
                    return new ListType(Type(list.Type)) { SourceLocation = Convert(list.Location) };
                }
            }

            throw new InvalidOperationException($"Unmapped type {type.Kind}");
        }

        private static CommentNode Comment(GraphQLComment comment)
        {
            return comment == null
                ? null
                : new CommentNode(comment.Text) { SourceLocation = Convert(comment.Location) };
        }

        public static OperationType ToOperationType(OperationTypeParser type) => type switch
        {
            OperationTypeParser.Query => OperationType.Query,
            OperationTypeParser.Mutation => OperationType.Mutation,
            OperationTypeParser.Subscription => OperationType.Subscription,
            _ => throw new InvalidOperationException($"Unmapped operation type {type}")
        };

        private static SourceLocation Convert(GraphQLLocation location) => new SourceLocation(location.Start, location.End);
    }

    // * DESCRIPTION TAKEN FROM MS REFERENCE SOURCE *
    // https://github.com/microsoft/referencesource/blob/master/mscorlib/system/decimal.cs
    // The lo, mid, hi, and flags fields contain the representation of the
    // Decimal value. The lo, mid, and hi fields contain the 96-bit integer
    // part of the Decimal. Bits 0-15 (the lower word) of the flags field are
    // unused and must be zero; bits 16-23 contain must contain a value between
    // 0 and 28, indicating the power of 10 to divide the 96-bit integer part
    // by to produce the Decimal value; bits 24-30 are unused and must be zero;
    // and finally bit 31 indicates the sign of the Decimal value, 0 meaning
    // positive and 1 meaning negative.
    internal readonly struct DecimalData
    {
        public readonly uint Flags;
        public readonly uint Hi;
        public readonly uint Lo;
        public readonly uint Mid;

        internal DecimalData(uint flags, uint hi, uint lo, uint mid)
        {
            Flags = flags;
            Hi = hi;
            Lo = lo;
            Mid = mid;
        }

        internal bool Equals(in DecimalData other) => Flags == other.Flags && Hi == other.Hi && Lo == other.Lo && Mid == other.Mid;
    }
}
