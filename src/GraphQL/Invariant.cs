using System;

namespace GraphQL
{
    public static class Invariant
    {
        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if <c>valid</c> is false or <c>message</c> is empty.
        /// </summary>
        public static void Check(bool valid, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new InvalidOperationException("Invariant requires an error message.");
            }

            if (!valid)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
