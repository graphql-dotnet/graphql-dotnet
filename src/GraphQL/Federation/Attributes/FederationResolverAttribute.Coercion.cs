using System.Collections;
using System.Diagnostics;
using GraphQL.Federation.Resolvers;
using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL.Federation;

public partial class FederationResolverAttribute
{
    private partial class FederationStaticResolver : IFederationResolver
    {
        /// <summary>
        /// Deserializes an argument supplied within an entity representation from Apollo Router.
        /// </summary>
        /// <remarks>
        /// Also see <see cref="ValidationContext.GetVariableValueAsync"/>.
        /// </remarks>
        private static object? Deserialize(IGraphType type, string name, object? value)
        {
            // parse recursively based on the type of the graph type
            return ParseValue(type, name, value);

            static object? ParseValue(IGraphType type, VariableName name, object? value)
            {
                // validate non-null types and values first
                if (type is NonNullGraphType nonNullGraphType)
                {
                    return value == null
                        ? ThrowValueIsNullException(name)
                        : ParseValue(nonNullGraphType.ResolvedType!, name, value);
                }
                else if (value == null)
                    return null;

                // return a value based on the type of the graph type (scalar, input object, list)
                return type switch
                {
                    // scalar types simply parse the value
                    ScalarGraphType scalarGraphType => scalarGraphType.ParseValue(value),

                    // input object types need to parse each field first, then call ParseDictionary
                    IInputObjectGraphType inputObjectGraphType => value is IDictionary<string, object?> dic
                        ? ParseValueObject(inputObjectGraphType, name, dic)
                        : ThrowNotDictionaryException(name),

                    // list types need to parse each item in the list
                    ListGraphType listGraphType => value is IList list && value is not string
                        ? ParseValueList(listGraphType, name, list)
                        : new object?[] { ParseValue(listGraphType.ResolvedType!, name, value) },

                    // there should not be any other types remaining
                    _ => ThrowNotInputTypeException(name),
                };
            }

            // parse a list of values, returning an array of parsed objects
            static object? ParseValueList(ListGraphType listGraphType, VariableName name, IList list)
            {
                var itemType = listGraphType.ResolvedType!;
                var ret = new object?[list.Count];
                for (var i = 0; i < list.Count; i++)
                {
                    ret[i] = ParseValue(itemType, new(name, i), list[i]);
                }
                return ret;
            }

            // parse a dictionary of values, returning a parsed object
            static object? ParseValueObject(IInputObjectGraphType inputObjectGraphType, VariableName name, IDictionary<string, object?> dic)
            {
                bool anyNull = false;
                int fieldCount = 0;
                var ret = new Dictionary<string, object?>();
                foreach (var field in inputObjectGraphType.Fields)
                {
                    // if the field is present in the dictionary, parse the value
                    // if the field is not present, use the default value if it exists
                    // if the field is required and not present, throw an exception
                    var key = field.Name;
                    if (dic.TryGetValue(key, out var value))
                    {
                        ret[key] = ParseValue(field.ResolvedType!, new(name, key), value);
                        fieldCount += 1;
                        anyNull |= value == null;
                    }
                    else if (field.DefaultValue != null)
                    {
                        ret[key] = field.DefaultValue;
                    }
                    else if (field.ResolvedType is NonNullGraphType)
                    {
                        ThrowMissingFieldException(new(name, key));
                    }
                }

                // if the input object type is a one-of type, there must be exactly one field present
                if (inputObjectGraphType.IsOneOf && (fieldCount != 1 || anyNull))
                    ThrowOneOfException(name);

                // check for unmatched fields in the dictionary
                foreach (var key in dic.Keys)
                {
                    bool match = false;
                    foreach (var field in inputObjectGraphType.Fields)
                    {
                        if (key == field.Name)
                        {
                            match = true;
                            break;
                        }
                    }

                    if (!match)
                    {
                        ThrowExcessFieldException(name, key);
                    }
                }

                // parse the dictionary into a CLR object and return it
                return inputObjectGraphType.ParseDictionary(ret);
            }

            [StackTraceHidden]
            [DoesNotReturn]
            static object? ThrowValueIsNullException(VariableName name)
                => throw new InvalidOperationException($"The argument '{name}' must not be null.");

            [StackTraceHidden]
            [DoesNotReturn]
            static object? ThrowNotDictionaryException(VariableName name)
                => throw new InvalidOperationException($"The argument '{name}' must be a dictionary.");

            [StackTraceHidden]
            [DoesNotReturn]
            static object? ThrowNotInputTypeException(VariableName name)
                => throw new InvalidOperationException($"The argument '{name}' must be an input type.");

            [StackTraceHidden]
            [DoesNotReturn]
            static void ThrowMissingFieldException(VariableName name)
                => throw new InvalidOperationException($"The argument '{name}' is required but was not provided.");

            [StackTraceHidden]
            [DoesNotReturn]
            static void ThrowOneOfException(VariableName name)
                => throw new InvalidOperationException($"The argument '{name}' must have exactly one field present.");

            [StackTraceHidden]
            [DoesNotReturn]
            static void ThrowExcessFieldException(VariableName name, string field)
                => throw new InvalidOperationException($"The argument '{name}' has an excess field named {field}.");
        }
    }
}
