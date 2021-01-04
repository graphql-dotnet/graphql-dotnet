using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using GraphQL.Language.AST;
using GraphQLParser;
using GraphQLParser.AST;
using OperationType = GraphQL.Language.AST.OperationType;
using OperationTypeParser = GraphQLParser.AST.OperationType;

namespace GraphQL.Language
{
    /// <summary>
    /// Converts an GraphQLParser AST representation of a document into a GraphQL.NET AST
    /// representation of a document.
    /// </summary>
    public class CoreToVanillaConverter
    {
        private readonly ISource _body;

        internal CoreToVanillaConverter(string body)
        {
            _body = new Source(body);
        }

        /// <summary>
        /// Converts an GraphQLParser AST representation of a document into a GraphQL.NET AST
        /// representation of a document and returns it.
        /// </summary>
        public static Document Convert(string body, GraphQLDocument source)
        {
            var converter = new CoreToVanillaConverter(body);
            var target = new Document();
            converter.AddDefinitions(source, target);
            return target;
        }

        /// <summary>
        /// Enumerates the operations and fragments in the source document and adds them to the target document.
        /// </summary>
        public void AddDefinitions(GraphQLDocument source, Document target)
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

        /// <summary>
        /// Converts an operation node and its children.
        /// </summary>
        public Operation Operation(GraphQLOperationDefinition source)
        {
            var name = source.Name != null ? Name(source.Name) : null;
            var op = new Operation(name).WithLocation(source);
            op.CommentNode = Comment(source.Comment);
            op.OperationType = ToOperationType(source.Operation);
            op.SelectionSet = SelectionSet(source.SelectionSet);
            op.Variables = VariableDefinitions(source.VariableDefinitions);
            op.Directives = Directives(source.Directives);
            return op;
        }

        /// <summary>
        /// Converts a fragment definition node and its children.
        /// </summary>
        public FragmentDefinition Fragment(GraphQLFragmentDefinition source)
        {
            var frag = new FragmentDefinition(Name(source.Name)).WithLocation(source);
            frag.CommentNode = Comment(source.Comment);
            frag.Type = NamedType(source.TypeCondition);
            frag.SelectionSet = SelectionSet(source.SelectionSet);
            frag.Directives = Directives(source.Directives);
            return frag;
        }

        /// <summary>
        /// Converts a fragment spread node and its children.
        /// </summary>
        public FragmentSpread FragmentSpread(GraphQLFragmentSpread source)
        {
            var name = source.Name != null ? Name(source.Name) : null;
            var spread = new FragmentSpread(name).WithLocation(source);
            spread.CommentNode = Comment(source.Comment);
            spread.Directives = Directives(source.Directives);
            return spread;
        }

        /// <summary>
        /// Converts an inline fragment node and its children.
        /// </summary>
        public InlineFragment InlineFragment(GraphQLInlineFragment source)
        {
            var frag = new InlineFragment().WithLocation(source);
            frag.CommentNode = Comment(source.Comment);
            frag.Type = source.TypeCondition != null ? NamedType(source.TypeCondition) : null;
            frag.Directives = Directives(source.Directives);
            frag.SelectionSet = SelectionSet(source.SelectionSet);
            return frag;
        }

        /// <summary>
        /// Converts a list of variable definition nodes and their children.
        /// </summary>
        public VariableDefinitions VariableDefinitions(IEnumerable<GraphQLVariableDefinition> source)
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

        /// <summary>
        /// Converts a variable definition node and its children.
        /// </summary>
        public VariableDefinition VariableDefinition(GraphQLVariableDefinition source)
        {
            var def = new VariableDefinition(Name(source.Variable.Name)).WithLocation(source);
            def.CommentNode = Comment(source.Comment);
            def.Type = Type(source.Type);
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

        /// <summary>
        /// Converts a selection set node and its children.
        /// </summary>
        public SelectionSet SelectionSet(GraphQLSelectionSet source)
        {
            var set = new SelectionSet().WithLocation(source);

            if (source != null)
            {
                foreach (var s in source.Selections)
                {
                    set.Add(Selection(s));

                }
            }

            return set;
        }

        /// <summary>
        /// Converts a selection node and its children.
        /// </summary>
        public ISelection Selection(ASTNode source) => source.Kind switch
        {
            ASTNodeKind.Field => Field((GraphQLFieldSelection)source),
            ASTNodeKind.FragmentSpread => FragmentSpread((GraphQLFragmentSpread)source),
            ASTNodeKind.InlineFragment => InlineFragment((GraphQLInlineFragment)source),
            _ => throw new InvalidOperationException($"Unmapped selection {source.Kind}")
        };

        /// <summary>
        /// Converts a field node and its children.
        /// </summary>
        public Field Field(GraphQLFieldSelection source)
        {
            var alias = source.Alias != null ? Name(source.Alias) : null;
            var field = new Field(alias, Name(source.Name)).WithLocation(source);
            field.CommentNode = Comment(source.Comment);
            field.Arguments = Arguments(source.Arguments);
            field.Directives = Directives(source.Directives);
            field.SelectionSet = SelectionSet(source.SelectionSet);
            return field;
        }

        /// <summary>
        /// Converts a list of directive nodes and their children.
        /// </summary>
        public Directives Directives(IEnumerable<GraphQLDirective> source)
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

        /// <summary>
        /// Converts a directive node and its children.
        /// </summary>
        public Directive Directive(GraphQLDirective d)
        {
            var dir = new Directive(Name(d.Name)).WithLocation(d);
            dir.Arguments = Arguments(d.Arguments);
            return dir;
        }

        /// <summary>
        /// Converts a list of argument nodes and their children.
        /// </summary>
        public Arguments Arguments(IEnumerable<GraphQLArgument> source)
        {
            Arguments target = null;

            if (source != null)
            {
                foreach (var a in source)
                {
                    var arg = new Argument(Name(a.Name)).WithLocation(a.Name);
                    arg.CommentNode = Comment(a.Comment);
                    arg.Value = Value(a.Value);
                    (target ??= new Arguments()).Add(arg);
                }
            }

            return target;
        }

        /// <summary>
        /// Converts a value node and its children.
        /// </summary>
        public IValue Value(GraphQLValue source)
        {
            switch (source.Kind)
            {
                case ASTNodeKind.StringValue:
                {
                    var str = (GraphQLScalarValue)source;
                    return new StringValue(str.Value).WithLocation(str);
                }
                case ASTNodeKind.IntValue:
                {
                    var str = (GraphQLScalarValue)source;

                    if (int.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var intResult))
                    {
                        return new IntValue(intResult).WithLocation(str);
                    }

                    // If the value doesn't fit in an integer, revert to using long...
                    if (long.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var longResult))
                    {
                        return new LongValue(longResult).WithLocation(str);
                    }

                    // If the value doesn't fit in an long, revert to using decimal...
                    if (decimal.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var decimalResult))
                    {
                        return new DecimalValue(decimalResult).WithLocation(str);
                    }

                    // If the value doesn't fit in an decimal, revert to using BigInteger...
                    if (BigInteger.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var bigIntegerResult))
                    {
                        return new BigIntValue(bigIntegerResult).WithLocation(str);
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
                            return new DecimalValue(dec).WithLocation(str);
                    }

                    return new FloatValue(dbl).WithLocation(str);
                }
                case ASTNodeKind.BooleanValue:
                {
                    var str = (GraphQLScalarValue)source;
                    return new BooleanValue(bool.Parse(str.Value)).WithLocation(str);
                }
                case ASTNodeKind.EnumValue:
                {
                    var str = (GraphQLScalarValue)source;
                    return new EnumValue(str.Value).WithLocation(str);
                }
                case ASTNodeKind.Variable:
                {
                    var vari = (GraphQLVariable)source;
                    return new VariableReference(Name(vari.Name)).WithLocation(vari);
                }
                case ASTNodeKind.ObjectValue:
                {
                    var obj = (GraphQLObjectValue)source;
                    var fields = obj.Fields?.Select(ObjectField);
                    return new ObjectValue(fields).WithLocation(obj);
                }
                case ASTNodeKind.ListValue:
                {
                    var list = (GraphQLListValue)source;
                    var values = list.Values?.Select(Value);
                    return new ListValue(values).WithLocation(list);
                }
                case ASTNodeKind.NullValue:
                {
                    var str = (GraphQLScalarValue)source;
                    return new NullValue().WithLocation(str);
                }
            }

            throw new InvalidOperationException($"Unmapped value type {source.Kind}");
        }

        /// <summary>
        /// Converts and object field node and its children.
        /// </summary>
        public ObjectField ObjectField(GraphQLObjectField source)
        {
            return new ObjectField(Name(source.Name), Value(source.Value)).WithLocation(source);
        }

        /// <summary>
        /// Converts a named type node and its children.
        /// </summary>
        public NamedType NamedType(GraphQLNamedType source)
        {
            return new NamedType(Name(source.Name)).WithLocation(source);
        }

        /// <summary>
        /// Converts a type node and its children.
        /// </summary>
        public IType Type(GraphQLType type)
        {
            switch (type.Kind)
            {
                case ASTNodeKind.NamedType:
                {
                    var name = (GraphQLNamedType)type;
                    return new NamedType(Name(name.Name)).WithLocation(name);
                }

                case ASTNodeKind.NonNullType:
                {
                    var nonNull = (GraphQLNonNullType)type;
                    return new NonNullType(Type(nonNull.Type)).WithLocation(nonNull);
                }

                case ASTNodeKind.ListType:
                {
                    var list = (GraphQLListType)type;
                    return new ListType(Type(list.Type)).WithLocation(list);
                }
            }

            throw new InvalidOperationException($"Unmapped type {type.Kind}");
        }

        /// <summary>
        /// Converts a name node.
        /// </summary>
        public NameNode Name(GraphQLName name)
        {
            return new NameNode(name.Value).WithLocation(name);
        }

        /// <summary>
        /// Converts a comment node.
        /// </summary>
        private CommentNode Comment(GraphQLComment comment)
        {
            if (comment == null)
                return null;

            return new CommentNode(comment.Text).WithLocation(comment);
        }

        /// <summary>
        /// Converts an operation type enumeration value.
        /// </summary>
        public static OperationType ToOperationType(OperationTypeParser type) => type switch
        {
            OperationTypeParser.Query => OperationType.Query,
            OperationTypeParser.Mutation => OperationType.Mutation,
            OperationTypeParser.Subscription => OperationType.Subscription,
            _ => throw new InvalidOperationException($"Unmapped operation type {type}")
        };
    }

    /// <summary>
    /// Provides helper methods for converting GraphQLParser AST documents to GraphQL.NET AST documents.
    /// </summary>
    public static class AstNodeExtensions
    {
        /// <summary>
        /// Copies the source location information from <paramref name="astNode"/> to <paramref name="node"/>.
        /// </summary>
        public static T WithLocation<T>(this T node, ASTNode astNode)
            where T : AbstractNode
        {
            return node.WithLocation(0, 0, astNode?.Location.Start ?? -1, astNode?.Location.End ?? -1);
        }
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
