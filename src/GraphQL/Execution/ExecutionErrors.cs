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

        public void AddRange(IEnumerable<ExecutionError> errors)
        {
            _errors.AddRange(errors);
        }

        public int Count
        {
            get { return _errors.Count; }
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
