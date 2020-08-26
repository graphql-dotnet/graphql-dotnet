using GraphQL.Language.AST;

namespace GraphQL.Validation.Errors
{
    public class NoFragmentCyclesError : ValidationError
    {
        public const string PARAGRAPH = "5.5.2.2";

        public NoFragmentCyclesError(ValidationContext context, string fragName, string[] spreadNames, params INode[] nodes)
            : base(context.OriginalQuery, PARAGRAPH, CycleErrorMessage(fragName, spreadNames), nodes)
        {
        }

        internal static string CycleErrorMessage(string fragName, string[] spreadNames)
        {
            var via = spreadNames.Length > 0 ? " via " + string.Join(", ", spreadNames) : "";
            return $"Cannot spread fragment \"{fragName}\" within itself{via}.";
        }
    }
}
