using System;
using GraphQL.Types;

namespace GraphQL.Conversion
{
    /// <summary>
    /// PascalCase name converter.
    /// </summary>
    public class PascalCaseNameConverter : INameConverter
    {
        /// <summary>
        /// Static instance of <see cref="PascalCaseNameConverter"/> that can be reused instead of creating new.
        /// </summary>
        public static readonly PascalCaseNameConverter Instance = new PascalCaseNameConverter();

        /// <summary>
        /// Returns the field name converted to PascalCase.
        /// </summary>
        public string NameForField(string fieldName, IComplexGraphType graphType) => fieldName.ToPascalCase();

        /// <summary>
        /// Returns the argument name converted to PascalCase.
        /// </summary>
        public string NameForArgument(string argumentName, IComplexGraphType graphType, FieldType field) => argumentName.ToPascalCase();
    }
}
