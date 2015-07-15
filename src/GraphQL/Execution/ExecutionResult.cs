namespace GraphQL
{
    public class ExecutionResult
    {
        public object Data { get; set; }

        public ExecutionErrors Errors { get; set; }
    }
}