using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Language
{
    /// <summary>
    /// Converts an GraphQLParser AST representation of a document into a GraphQL.NET AST
    /// representation of a document. Works only with executable definitions - operations
    /// and fragments, all other definitions are ignored.
    /// <br/>
    /// For more information see https://spec.graphql.org/June2018/#sec-Language.Document.
    /// </summary>
    public class CoreToVanillaConverter : DefaultNodeVisitor<CoreToVanillaConverterContext>
    {
        public override async ValueTask Visit(ASTNode? node, CoreToVanillaConverterContext context)
        {
            if (node == null)
                return;

            if (node is GraphQLValue value)
            {
                var converted = Value(value);

                var parent = context.Parents.Peek();
                if (parent is GraphQLObjectField objField)
                    objField.Value = converted;
                else if (parent is GraphQLVariableDefinition def)
                    def.DefaultValue = converted;
                else
                    throw new NotSupportedException();
            }

            context.Parents.Push(node);

            await base.Visit(node, context).ConfigureAwait(false);

            context.Parents.Pop();
        }

        /// <summary>
        /// Converts a value node and its children.
        /// </summary>
        internal static GraphQLValue Value(GraphQLValue source)
        {
            switch (source.Kind)
            {
                case ASTNodeKind.StringValue:
                {
                    var str = (GraphQLStringValue)source;
                    return new StringValue((string)str.Value) { Value = str.Value, Location = str.Location };
                }
                case ASTNodeKind.IntValue:
                {
                    var str = (GraphQLIntValue)source;

                    if (Int.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int intResult))
                    {
                        return new IntValue(intResult) { Value = str.Value, Location = str.Location };
                    }

                    // If the value doesn't fit in an integer, revert to using long...
                    if (Long.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long longResult))
                    {
                        return new LongValue(longResult) { Value = str.Value, Location = str.Location };
                    }

                    // If the value doesn't fit in an long, revert to using decimal...
                    if (Decimal.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal decimalResult))
                    {
                        return new DecimalValue(decimalResult) { Value = str.Value, Location = str.Location };
                    }

                    // If the value doesn't fit in an decimal, revert to using BigInteger...
                    if (BigInt.TryParse(str.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var bigIntegerResult))
                    {
                        return new BigIntValue(bigIntegerResult) { Value = str.Value, Location = str.Location };
                    }

                    // Since BigInteger can contain any valid integer (arbitrarily large), this is impossible to trigger via an invalid query
                    throw new InvalidOperationException($"Invalid number {str.Value}");
                }
                case ASTNodeKind.FloatValue:
                {
                    var str = (GraphQLFloatValue)source;

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
                            return new DecimalValue(dec) { Value = str.Value, Location = str.Location };
                    }

                    return new FloatValue(dbl) { Value = str.Value, Location = str.Location };
                }
                case ASTNodeKind.BooleanValue:
                {
                    var str = (GraphQLBooleanValue)source;
                    return new BooleanValue(str.Value.Length == 4 /*true.Length=4*/) { Value = str.Value, Location = str.Location };
                }
                case ASTNodeKind.EnumValue:
                {
                    var str = (GraphQLEnumValue)source;
                    return new EnumValue(str.Name) { Location = str.Location };
                }
                case ASTNodeKind.Variable:
                {
                    var vari = (GraphQLVariable)source;
                    return new VariableReference { Name = vari.Name, Location = vari.Location };
                }
                case ASTNodeKind.ObjectValue:
                {
                    var obj = (GraphQLObjectValue)source;
                    if (obj.Fields == null || obj.Fields.Count == 0)
                        return source;

                    foreach (var field in obj.Fields)
                        field.Value = Value(field.Value);

                    return new GraphQLObjectValue { Fields = obj.Fields, Location = obj.Location };
                }
                case ASTNodeKind.ListValue:
                {
                    var list = (GraphQLListValue)source;
                    var values = list.Values?.Select(Value).Cast<IValue>() ?? Enumerable.Empty<IValue>();
                    return new ListValue(values) { Value = list.Value, Values = list.Values, Location = list.Location };
                }
                case ASTNodeKind.NullValue:
                {
                    var str = (GraphQLNullValue)source;
                    return new NullValue { Value = str.Value, Location = str.Location };
                }
            }

            throw new InvalidOperationException($"Unmapped value type {source.Kind}");
        }
    }

    public class CoreToVanillaConverterContext : INodeVisitorContext
    {
        public CancellationToken CancellationToken => default;

        public Stack<ASTNode> Parents { get; } = new Stack<ASTNode>();
    }
}
