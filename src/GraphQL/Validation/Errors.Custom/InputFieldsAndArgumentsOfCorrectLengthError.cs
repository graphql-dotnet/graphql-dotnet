using System;
using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.InputFieldsAndArgumentsOfCorrectLength"/>
    [Serializable]
    public class InputFieldsAndArgumentsOfCorrectLengthError : ValidationError
    {
        private const string NUMBER = "5.6.1";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public InputFieldsAndArgumentsOfCorrectLengthError(ValidationContext context, INode node, int? length, int? min, int? max)
            : base(context.Document.OriginalQuery, NUMBER, BadValueMessage(node, length, min, max), node)
        {
        }

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public InputFieldsAndArgumentsOfCorrectLengthError(ValidationContext context, VariableDefinition node, VariableName variableName, int? length, int? min, int? max)
            : base(context.Document.OriginalQuery, NUMBER, BadValueMessage(variableName, length, min, max), node)
        {
        }

        private static string BadValueMessage(INode node, int? length, int? minLength, int? maxLength)
        {
            string len = length.HasValue ? length.ToString() : "null";
            string min = (minLength ?? 0).ToString();
            string max = maxLength.HasValue ? maxLength.ToString() : "unrestricted";
            return $"{node.GetType().Name} '{((IHaveName)node).NameNode.Name}' has invalid length ({len}). Length must be in range [{min}, {max}].";
        }

        private static string BadValueMessage(VariableName variableName, int? length, int? minLength, int? maxLength)
        {
            string len = length.HasValue ? length.ToString() : "null";
            string min = (minLength ?? 0).ToString();
            string max = maxLength.HasValue ? maxLength.ToString() : "unrestricted";
            return $"Variable '{variableName}' has invalid length ({len}). Length must be in range [{min}, {max}].";
        }
    }
}
