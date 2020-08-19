using System;

namespace GraphQL.Execution
{
    [Serializable]
    public class NoOperationError : DocumentError
    {
        public NoOperationError()
            : base("Document does not contain any operations.")
        {
            Code = "NO_OPERATION";
        }
    }
}
