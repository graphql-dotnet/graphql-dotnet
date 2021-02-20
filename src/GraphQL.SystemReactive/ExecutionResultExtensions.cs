namespace GraphQL.Execution
{
    internal static class ExecutionResultExtensions
    {
        public static ExecutionResult With(this ExecutionResult result, ExecutionContext context)
        {
            result.Query = context.Document.OriginalQuery;
            result.Document = context.Document;
            result.Operation = context.Operation;
            result.Extensions = context.Extensions;

            return result;
        }
    }
}
