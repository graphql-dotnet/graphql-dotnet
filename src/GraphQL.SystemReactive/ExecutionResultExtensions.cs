namespace GraphQL.Execution
{
    internal static class ExecutionResultExtensions
    {
        public static ExecutionResult With(this ExecutionResult result, ExecutionContext context)
        {
            result.Query = context.OriginalQuery;
            result.Document = context.Document;
            result.Operation = context.Operation;
            result.Extensions = context.OutputExtensions;

            return result;
        }
    }
}
