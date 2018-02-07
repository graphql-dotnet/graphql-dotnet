namespace GraphQL
{
    public static class Invariant
    {
        /// <summary>
        /// Throws an ExecutionError if <c>valid</c> is false or <c>message</c> is empty.
        /// </summary>
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
