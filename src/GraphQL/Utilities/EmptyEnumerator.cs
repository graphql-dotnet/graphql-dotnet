using System.Collections;
using System.Collections.Generic;

namespace GraphQL.Utilities
{
    internal sealed class EmptyEnumerator<T> : IEnumerator<T>
    {
        public static readonly EmptyEnumerator<T> Instance = new EmptyEnumerator<T>();

        private EmptyEnumerator() { }

        public T Current => default;

        object IEnumerator.Current => default;

        public void Dispose() {}

        public bool MoveNext() => false;

        public void Reset() { }
    }
}
