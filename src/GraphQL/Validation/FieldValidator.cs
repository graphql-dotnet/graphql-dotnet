using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphQL.Validation
{
    public class FieldValidator: IFieldValidator
    {
        public ExecutionErrors Validate(ISchema schema, IReadOnlyList<ValidationFrame> fieldStack)
        {
            var errors = new ExecutionErrors();
            AssertValidValue(schema, fieldStack, errors);
            return errors;
        }

        public void AssertValidValue(ISchema schema,  IReadOnlyList<ValidationFrame> fieldStack, ExecutionErrors errors)
        {
            var item = fieldStack.Last();
            var fieldName = item.Name;
            var input = item.Value;
            var type = item.GraphType;
            if (type is NonNullGraphType graphType)
            {
                var nonNullType = graphType.ResolvedType;

                if (input == null)
                {
                    AddError("Received a null input for a non-null field.", fieldStack, errors);
                }
                else
                {
                    var newStack = AddFrame(fieldStack.Take(fieldStack.Count - 1), new ValidationFrame(fieldName, item.Value, nonNullType,null));
                    AssertValidValue(schema, newStack, errors);
                    return;
                }
            }

            if (input == null)
            {
                return;
            }

            if (type is ScalarGraphType scalar)
            {
                try
                {
                    if (ValueFromScalar(scalar, input) == null)
                        AddError("Invalid Scalar value for input field.", fieldStack, errors);
                }
                catch(Exception e)
                {
                    AddError($"Invalid Scalar value for input field: {e.Message}", fieldStack, errors);
                }
                return;
            }

            if (type is ListGraphType listType)
            {
                  var listItemType = listType.ResolvedType;

                 if (input is IEnumerable list && !(input is string))
                 {
                     var index = -1;
                    foreach (var listItem in list)
                    {
                        var newStack = AddFrame(fieldStack.Take(fieldStack.Count-1), new ValidationFrame(fieldName, listItem, listItemType, ++index));
                        AssertValidValue(schema, newStack, errors);
                    }
                 }
                 else
                 {
                    var newStack = AddFrame(fieldStack, new ValidationFrame(fieldName, input, listItemType, null));
                    AssertValidValue(schema, newStack, errors);
                 }
                return;
            }

            if (type is IObjectGraphType || type is IInputObjectGraphType)
            {
                var complexType = (IComplexGraphType)type;

                if (!(input is Dictionary<string, object> dict))
                {
                    AddError($"Unable to parse input as a '{type.Name}' type. Did you provide a List or Scalar value accidentally?", fieldStack, errors);
                    return;
                }
                else
                {
                    // ensure every provided field is defined
                    IList<string> unknownFields = null;

                    if (type is IInputObjectGraphType)
                    {
                        unknownFields = dict.Keys
                            .Except(complexType.Fields.Select(f => f.Name))
                            .ToList();
                    }

                    if (unknownFields?.Count > 0)
                    {
                        AddError($"Unrecognized input fields {string.Join(", ", unknownFields.Select(k => $"'{k}'"))} for type '{type.Name}'.", fieldStack, errors);
                    }

                    foreach (var field in complexType.Fields)
                    {
                        dict.TryGetValue(field.Name, out object fieldValue);
                        var newStack = AddFrame(fieldStack, new ValidationFrame(field.Name, fieldValue, field.ResolvedType, null));
                        AssertValidValue(schema,newStack, errors);
                    }
                    return;
                }
            }

            AddError("Invalid input", fieldStack, errors);
        }

        public virtual void AddError(string message , IReadOnlyList<ValidationFrame> fieldStack, ExecutionErrors errors)
        {
            var path = string.Join(".", fieldStack.Select(f => f.GetLocation()));
            errors.Add(new ExecutionError($"Variable '${path}' is invalid. {message}"));
        }

        private IReadOnlyList<ValidationFrame> AddFrame(IEnumerable<ValidationFrame> oldStack, ValidationFrame f)
        {
            var stack = new List<ValidationFrame>(oldStack);
            stack.Add(f);
            return stack;
        }

        private object ValueFromScalar(ScalarGraphType scalar, object input)
        {
            if (input is IValue value)
            {
                return scalar.ParseLiteral(value);
            }

            return scalar.ParseValue(input);
        }
    }
}
