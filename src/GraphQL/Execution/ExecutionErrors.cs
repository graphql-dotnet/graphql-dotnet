using System.Collections;
using System.Collections.Generic;

namespace GraphQL
{
    public class ExecutionErrors : IEnumerable<ExecutionError>
    {
        private readonly List<ExecutionError> _errors = new List<ExecutionError>();

        public void Add(ExecutionError error)
        {
            _errors.Add(error);
        }

        public IEnumerator<ExecutionError> GetEnumerator()
        {
            return _errors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
