namespace GraphQL.Types
{
    /// <summary>
    /// Allows to change the case of the enum names for enum marked with that attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
    public abstract class EnumCaseAttribute : Attribute
    {
        /// <summary>
        /// Changes the case of the specified enum name.
        /// </summary>
        public abstract string ChangeEnumCase(string val);
    }

    /// <summary>
    /// Returns a constant case version of enum names.
    /// For example, converts 'StringError' into 'STRING_ERROR'.
    /// </summary>
    public class ConstantCaseAttribute : EnumCaseAttribute
    {
        /// <inheritdoc />
        public override string ChangeEnumCase(string val) => val.ToConstantCase();
    }

    /// <summary>
    /// Returns a camel case version of enum names.
    /// </summary>
    public class CamelCaseAttribute : EnumCaseAttribute
    {
        /// <inheritdoc />
        public override string ChangeEnumCase(string val) => val.ToCamelCase();
    }

    /// <summary>
    /// Returns a pascal case version of enum names.
    /// </summary>
    public class PascalCaseAttribute : EnumCaseAttribute
    {
        /// <inheritdoc />
        public override string ChangeEnumCase(string val) => val.ToPascalCase();
    }
}
