namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a variable name and value tuple that has been gathered from the document and attached <see cref="Inputs"/>.
    /// </summary>
    public class Variable
    {
        /// <summary>
        /// Gets or sets the name of the variable.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the variable.
        /// </summary>
        public object Value { get; set; }
    }
}
