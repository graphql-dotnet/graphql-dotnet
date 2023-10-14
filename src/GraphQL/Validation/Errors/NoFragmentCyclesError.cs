using GraphQLParser.AST;

namespace GraphQL.Validation.Errors
{
    /// <inheritdoc cref="Rules.NoFragmentCycles"/>
    [Serializable]
    public class NoFragmentCyclesError : ValidationError
    {
        internal const string NUMBER = "5.5.2.2";

        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public NoFragmentCyclesError(ValidationContext context, string fragName, string[] spreadNames, params ASTNode[] nodes)
            : base(context.Document.Source, NUMBER, CycleErrorMessage(fragName, spreadNames), nodes)
        {
        }

        internal static string CycleErrorMessage(string fragName, string[] spreadNames)
        {
            var via = spreadNames.Length > 0 ? " via " + string.Join(", ", spreadNames) : "";
            return $"Cannot spread fragment '{fragName}' within itself{via}.";
        }
    }
}
