namespace GraphQL
{
    public static class Invariant
    {
        public static void Check(bool valid, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ExecutionError("Invariant requires an error message.");
            }

            if (!valid)
            {
                throw new ExecutionError(message);
            }
        }
    }
}
